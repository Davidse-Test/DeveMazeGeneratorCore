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
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep7 : IAlgorithm<Maze>
    {
        // SIMD Step 7: Add SIMD memory operations and vectorized map access patterns

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

                // SIMD Step 7: Use vectorized memory operations for bounds and map checking
                bool validLeft, validRight, validUp, validDown;
                if (Sse2.IsSupported)
                {
                    CheckDirectionsWithVectorizedMemory(map, cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }
                else
                {
                    CheckDirectionsFallback(map, cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }

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

                    bool actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown;
                    if (Sse2.IsSupported)
                    {
                        SelectDirectionWithSIMD(validLeft, validRight, validUp, validDown, chosenDirection,
                            out actuallyGoingLeft, out actuallyGoingRight, out actuallyGoingUp, out actuallyGoingDown);
                    }
                    else
                    {
                        SelectDirectionFallback(validLeft, validRight, validUp, validDown, chosenDirection,
                            out actuallyGoingLeft, out actuallyGoingRight, out actuallyGoingUp, out actuallyGoingDown);
                    }

                    int nextX, nextY, nextXInBetween, nextYInBetween;
                    if (Sse2.IsSupported)
                    {
                        CalculateCoordinatesWithSIMD(cur, actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown,
                            out nextX, out nextY, out nextXInBetween, out nextYInBetween);
                    }
                    else
                    {
                        CalculateCoordinatesFallback(cur, actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown,
                            out nextX, out nextY, out nextXInBetween, out nextYInBetween);
                    }

                    // SIMD Step 7: Vectorized map updates
                    UpdateMapWithSIMD(map, nextX, nextY, nextXInBetween, nextYInBetween, stackje, pixelChangedCallback, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDirectionsWithVectorizedMemory<M>(M map, MazePoint cur, int width, int height, 
            out bool validLeft, out bool validRight, out bool validUp, out bool validDown) where M : InnerMap
        {
            // Vectorize all coordinate calculations
            var currentPos = Vector128.Create(cur.X, cur.Y, cur.X, cur.Y);
            var offsets = Vector128.Create(-2, -2, 2, 2);
            var bounds = Vector128.Create(0, 0, width, height);
            
            var targetPositions = currentPos + offsets;
            
            // Vectorized bounds checking
            var boundsCheck1 = Vector128.GreaterThan(Vector128.Create(targetPositions[0], targetPositions[1], 0, 0), Vector128.Create(0, 0, 0, 0));
            var boundsCheck2 = Vector128.LessThan(Vector128.Create(targetPositions[2], targetPositions[3], 0, 0), Vector128.Create(width, height, 0, 0));
            
            bool boundsLeft = boundsCheck1[0] != 0;
            bool boundsUp = boundsCheck1[1] != 0;
            bool boundsRight = boundsCheck2[2] != 0;
            bool boundsDown = boundsCheck2[3] != 0;

            // Combine with map checking using SIMD mask operations
            var validityMask = Vector128.Create(
                (boundsLeft && !map[cur.X - 2, cur.Y]) ? (byte)1 : (byte)0,
                (boundsRight && !map[cur.X + 2, cur.Y]) ? (byte)1 : (byte)0,
                (boundsUp && !map[cur.X, cur.Y - 2]) ? (byte)1 : (byte)0,
                (boundsDown && !map[cur.X, cur.Y + 2]) ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            validLeft = validityMask.GetElement(0) != 0;
            validRight = validityMask.GetElement(1) != 0;
            validUp = validityMask.GetElement(2) != 0;
            validDown = validityMask.GetElement(3) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDirectionsFallback<M>(M map, MazePoint cur, int width, int height, 
            out bool validLeft, out bool validRight, out bool validUp, out bool validDown) where M : InnerMap
        {
            validLeft = cur.X - 2 > 0 && !map[cur.X - 2, cur.Y];
            validRight = cur.X + 2 < width && !map[cur.X + 2, cur.Y];
            validUp = cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2];
            validDown = cur.Y + 2 < height && !map[cur.X, cur.Y + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMapWithSIMD<M, TAction>(M map, int nextX, int nextY, int nextXInBetween, int nextYInBetween,
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Use SIMD to prepare coordinates for batch operations
            var coordinates = Vector128.Create(nextXInBetween, nextYInBetween, nextX, nextY);
            
            // Though we can't fully vectorize map access due to 2D indexing,
            // we can use SIMD for coordinate validation and preparation
            var boundsVector = Vector128.Create(map.Width, map.Height, map.Width, map.Height);
            var validCoords = Vector128.LessThan(coordinates, boundsVector);
            
            // Extract and apply the updates (still need individual map access)
            if (validCoords[0] != 0 && validCoords[1] != 0)
            {
                map[nextXInBetween, nextYInBetween] = true;
                pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
            }
            
            if (validCoords[2] != 0 && validCoords[3] != 0)
            {
                map[nextX, nextY] = true;
                pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
            }

            stackje.Push(new MazePoint(nextX, nextY));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CountValidDirectionsWithSIMD(bool validLeft, bool validRight, bool validUp, bool validDown)
        {
            var validVector = Vector128.Create(
                validLeft ? (byte)1 : (byte)0,
                validRight ? (byte)1 : (byte)0,
                validUp ? (byte)1 : (byte)0,
                validDown ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            if (Sse3.IsSupported)
            {
                var sum16 = Sse2.SumAbsoluteDifferences(validVector, Vector128<byte>.Zero);
                return sum16.GetElement(0) + sum16.GetElement(4);
            }
            else
            {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SelectDirectionWithSIMD(bool validLeft, bool validRight, bool validUp, bool validDown, int chosenDirection,
            out bool actuallyGoingLeft, out bool actuallyGoingRight, out bool actuallyGoingUp, out bool actuallyGoingDown)
        {
            var validVector = Vector128.Create(
                validLeft ? (byte)1 : (byte)0,
                validRight ? (byte)1 : (byte)0,
                validUp ? (byte)1 : (byte)0,
                validDown ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            int count0 = 0;
            int count1 = count0 + validVector.GetElement(0);
            int count2 = count1 + validVector.GetElement(1);
            int count3 = count2 + validVector.GetElement(2);

            var targetVector = Vector128.Create(count0, count1, count2, count3);
            var chosenVector = Vector128.Create(chosenDirection, chosenDirection, chosenDirection, chosenDirection);

            var equalMask = Vector128.Equals(targetVector, chosenVector);
            var resultMask = Vector128.BitwiseAnd(equalMask, validVector.AsInt32());

            actuallyGoingLeft = resultMask.GetElement(0) != 0;
            actuallyGoingRight = resultMask.GetElement(1) != 0;
            actuallyGoingUp = resultMask.GetElement(2) != 0;
            actuallyGoingDown = resultMask.GetElement(3) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SelectDirectionFallback(bool validLeft, bool validRight, bool validUp, bool validDown, int chosenDirection,
            out bool actuallyGoingLeft, out bool actuallyGoingRight, out bool actuallyGoingUp, out bool actuallyGoingDown)
        {
            int countertje = 0;

            actuallyGoingLeft = validLeft & chosenDirection == countertje;
            countertje += validLeft ? 1 : 0;

            actuallyGoingRight = validRight & chosenDirection == countertje;
            countertje += validRight ? 1 : 0;

            actuallyGoingUp = validUp & chosenDirection == countertje;
            countertje += validUp ? 1 : 0;

            actuallyGoingDown = validDown & chosenDirection == countertje;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateCoordinatesWithSIMD(MazePoint cur, bool actuallyGoingLeft, bool actuallyGoingRight, bool actuallyGoingUp, bool actuallyGoingDown,
            out int nextX, out int nextY, out int nextXInBetween, out int nextYInBetween)
        {
            var directionVector = Vector128.Create(
                actuallyGoingLeft ? -2 : 0,
                actuallyGoingRight ? 2 : 0,
                actuallyGoingUp ? -2 : 0,
                actuallyGoingDown ? 2 : 0
            );

            var directionVectorInBetween = Vector128.Create(
                actuallyGoingLeft ? -1 : 0,
                actuallyGoingRight ? 1 : 0,
                actuallyGoingUp ? -1 : 0,
                actuallyGoingDown ? 1 : 0
            );

            int xOffset = directionVector.GetElement(0) + directionVector.GetElement(1);
            int yOffset = directionVector.GetElement(2) + directionVector.GetElement(3);

            int xOffsetInBetween = directionVectorInBetween.GetElement(0) + directionVectorInBetween.GetElement(1);
            int yOffsetInBetween = directionVectorInBetween.GetElement(2) + directionVectorInBetween.GetElement(3);

            nextX = cur.X + xOffset;
            nextY = cur.Y + yOffset;
            nextXInBetween = cur.X + xOffsetInBetween;
            nextYInBetween = cur.Y + yOffsetInBetween;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateCoordinatesFallback(MazePoint cur, bool actuallyGoingLeft, bool actuallyGoingRight, bool actuallyGoingUp, bool actuallyGoingDown,
            out int nextX, out int nextY, out int nextXInBetween, out int nextYInBetween)
        {
            byte actuallyGoingLeftByte = Unsafe.As<bool, byte>(ref actuallyGoingLeft);
            byte actuallyGoingRightByte = Unsafe.As<bool, byte>(ref actuallyGoingRight);
            byte actuallyGoingUpByte = Unsafe.As<bool, byte>(ref actuallyGoingUp);
            byte actuallyGoingDownByte = Unsafe.As<bool, byte>(ref actuallyGoingDown);

            nextX = cur.X + actuallyGoingLeftByte * -2 + actuallyGoingRightByte * 2;
            nextY = cur.Y + actuallyGoingUpByte * -2 + actuallyGoingDownByte * 2;

            nextXInBetween = cur.X - actuallyGoingLeftByte + actuallyGoingRightByte;
            nextYInBetween = cur.Y - actuallyGoingUpByte + actuallyGoingDownByte;
        }
    }
}