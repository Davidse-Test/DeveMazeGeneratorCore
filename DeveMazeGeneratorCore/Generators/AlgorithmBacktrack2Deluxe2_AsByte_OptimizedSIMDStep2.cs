using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace DeveMazeGeneratorCore.Generators
{
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep2 : IAlgorithm<Maze>
    {
        public Maze GoGenerate<M, TAction>(int width, int height, int seed, IInnerMapFactory<M> mapFactory, IRandomFactory randomFactory, TAction pixelChangedCallback)
            where M : InnerMap
            where TAction : struct, IProgressAction
        {
            var innerMap = mapFactory.Create(width, height);
            var random = randomFactory.Create(seed);

            return GoGenerateInternal(innerMap, random, pixelChangedCallback);
        }

        private Maze GoGenerateInternal<M, TAction>(M map, IRandom random, TAction pixelChangedCallback) where M : InnerMap where TAction : struct, IProgressAction
        {
            long totSteps = (map.Width - 1L) / 2L * ((map.Height - 1L) / 2L);
            long currentStep = 1;

            int width = map.Width - 1;
            int height = map.Height - 1;

            var stackje = new Stack<MazePoint>();
            stackje.Push(new MazePoint(1, 1));
            map[1, 1] = true;

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            while (stackje.Count != 0)
            {
                MazePoint cur = stackje.Peek();

                // SIMD Step 1: Use SIMD for bounds checking
                bool validLeft, validRight, validUp, validDown;
                if (Sse2.IsSupported)
                {
                    CheckBoundsWithSIMD(cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }
                else
                {
                    CheckBoundsFallback(cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }

                // Continue with map checking
                validLeft = validLeft && !map[cur.X - 2, cur.Y];
                validRight = validRight && !map[cur.X + 2, cur.Y];
                validUp = validUp && !map[cur.X, cur.Y - 2];
                validDown = validDown && !map[cur.X, cur.Y + 2];

                // SIMD Step 2: Use SIMD for direction counting
                int targetCount;
                if (Sse2.IsSupported)
                {
                    targetCount = CountValidDirectionsWithSIMD(validLeft, validRight, validUp, validDown);
                }
                else
                {
                    targetCount = CountValidDirectionsFallback(validLeft, validRight, validUp, validDown);
                }

                if (targetCount == 0)
                {
                    stackje.Pop();
                }
                else
                {
                    var chosenDirection = random.Next(targetCount);
                    int countertje = 0;

                    bool actuallyGoingLeft = validLeft & chosenDirection == countertje;
                    byte actuallyGoingLeftByte = Unsafe.As<bool, byte>(ref actuallyGoingLeft);
                    countertje += validLeft ? 1 : 0;

                    bool actuallyGoingRight = validRight & chosenDirection == countertje;
                    byte actuallyGoingRightByte = Unsafe.As<bool, byte>(ref actuallyGoingRight);
                    countertje += validRight ? 1 : 0;

                    bool actuallyGoingUp = validUp & chosenDirection == countertje;
                    byte actuallyGoingUpByte = Unsafe.As<bool, byte>(ref actuallyGoingUp);
                    countertje += validUp ? 1 : 0;

                    bool actuallyGoingDown = validDown & chosenDirection == countertje;
                    byte actuallyGoingDownByte = Unsafe.As<bool, byte>(ref actuallyGoingDown);

                    var nextX = cur.X + actuallyGoingLeftByte * -2 + actuallyGoingRightByte * 2;
                    var nextY = cur.Y + actuallyGoingUpByte * -2 + actuallyGoingDownByte * 2;

                    var nextXInBetween = cur.X - actuallyGoingLeftByte + actuallyGoingRightByte;
                    var nextYInBetween = cur.Y - actuallyGoingUpByte + actuallyGoingDownByte;

                    stackje.Push(new MazePoint(nextX, nextY));
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBoundsWithSIMD(MazePoint cur, int width, int height, out bool validLeft, out bool validRight, out bool validUp, out bool validDown)
        {
            // Use SIMD to check all bounds at once
            var coords = Vector128.Create(cur.X, cur.Y, cur.X, cur.Y);
            var bounds = Vector128.Create(2, 2, width - 2, height - 2);
            var offsets = Vector128.Create(-2, -2, 2, 2);
            
            var targets = coords + offsets;
            
            var leftUpComparison = Vector128.GreaterThan(Vector128.Create(targets[0], targets[1], 0, 0), Vector128.Create(bounds[0], bounds[1], 0, 0));
            var rightDownComparison = Vector128.LessThan(Vector128.Create(targets[2], targets[3], 0, 0), Vector128.Create(bounds[2], bounds[3], 0, 0));
            
            validLeft = leftUpComparison[0] != 0;
            validUp = leftUpComparison[1] != 0;
            validRight = rightDownComparison[2] != 0;
            validDown = rightDownComparison[3] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBoundsFallback(MazePoint cur, int width, int height, out bool validLeft, out bool validRight, out bool validUp, out bool validDown)
        {
            validLeft = cur.X - 2 > 0;
            validRight = cur.X + 2 < width;
            validUp = cur.Y - 2 > 0;
            validDown = cur.Y + 2 < height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CountValidDirectionsWithSIMD(bool validLeft, bool validRight, bool validUp, bool validDown)
        {
            // Pack booleans into a vector as bytes
            var validVector = Vector128.Create(
                validLeft ? (byte)1 : (byte)0,
                validRight ? (byte)1 : (byte)0,
                validUp ? (byte)1 : (byte)0,
                validDown ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            // Use SIMD horizontal sum to count valid directions
            if (Sse3.IsSupported)
            {
                // Use HADD for horizontal sum
                var sum16 = Sse2.SumAbsoluteDifferences(validVector, Vector128<byte>.Zero);
                return sum16.GetElement(0) + sum16.GetElement(4);
            }
            else
            {
                // Fallback: extract elements and sum manually
                return validVector.GetElement(0) + validVector.GetElement(1) + 
                       validVector.GetElement(2) + validVector.GetElement(3);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CountValidDirectionsFallback(bool validLeft, bool validRight, bool validUp, bool validDown)
        {
            int validLeftByte = Unsafe.As<bool, byte>(ref validLeft);
            int validRightByte = Unsafe.As<bool, byte>(ref validRight);
            int validUpByte = Unsafe.As<bool, byte>(ref validUp);
            int validDownByte = Unsafe.As<bool, byte>(ref validDown);

            return validLeftByte + validRightByte + validUpByte + validDownByte;
        }
    }
}