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
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep9 : IAlgorithm<Maze>
    {
        // SIMD Step 9: Add vectorized map access patterns and advanced SIMD operations

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

                // SIMD Step 9: Use advanced vectorized access patterns
                bool validLeft, validRight, validUp, validDown;
                if (Avx2.IsSupported)
                {
                    CheckDirectionsWithAVX2(map, cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }
                else if (Sse2.IsSupported)
                {
                    CheckDirectionsWithAdvancedSIMD(map, cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }
                else
                {
                    CheckDirectionsFallback(map, cur, width, height, out validLeft, out validRight, out validUp, out validDown);
                }

                int targetCount;
                if (Avx2.IsSupported)
                {
                    targetCount = CountValidDirectionsWithAVX2(validLeft, validRight, validUp, validDown);
                }
                else if (Sse2.IsSupported)
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
                    if (Avx2.IsSupported)
                    {
                        SelectDirectionWithAVX2(validLeft, validRight, validUp, validDown, chosenDirection,
                            out actuallyGoingLeft, out actuallyGoingRight, out actuallyGoingUp, out actuallyGoingDown);
                    }
                    else if (Sse2.IsSupported)
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
                    if (Avx2.IsSupported)
                    {
                        CalculateCoordinatesWithAVX2(cur, actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown,
                            out nextX, out nextY, out nextXInBetween, out nextYInBetween);
                    }
                    else if (Sse2.IsSupported)
                    {
                        CalculateCoordinatesWithSIMD(cur, actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown,
                            out nextX, out nextY, out nextXInBetween, out nextYInBetween);
                    }
                    else
                    {
                        CalculateCoordinatesFallback(cur, actuallyGoingLeft, actuallyGoingRight, actuallyGoingUp, actuallyGoingDown,
                            out nextX, out nextY, out nextXInBetween, out nextYInBetween);
                    }

                    UpdateMapWithVectorizedPattern(map, nextX, nextY, nextXInBetween, nextYInBetween, stackje, pixelChangedCallback, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDirectionsWithAVX2<M>(M map, MazePoint cur, int width, int height, 
            out bool validLeft, out bool validRight, out bool validUp, out bool validDown) where M : InnerMap
        {
            // Use AVX2 for 256-bit operations when available
            var currentPos = Vector256.Create(cur.X, cur.Y, cur.X, cur.Y, cur.X, cur.Y, cur.X, cur.Y);
            var offsets = Vector256.Create(-2, -2, 2, 2, 0, -2, 0, 2);
            var bounds = Vector256.Create(0, 0, width, height, 0, 0, width, height);
            
            var targetPositions = Avx2.Add(currentPos, offsets);
            
            // Advanced bounds checking with AVX2
            var boundsCheckMin = Avx2.CompareGreaterThan(targetPositions, Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0));
            var boundsCheckMax = Avx2.CompareGreaterThan(bounds, targetPositions);
            var validBounds = Avx2.And(boundsCheckMin, boundsCheckMax);
            
            bool boundsLeft = validBounds.GetElement(0) != 0;
            bool boundsUp = validBounds.GetElement(1) != 0;
            bool boundsRight = validBounds.GetElement(2) != 0;
            bool boundsDown = validBounds.GetElement(3) != 0;

            // Map checking with vectorized pattern
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
        private void CheckDirectionsWithAdvancedSIMD<M>(M map, MazePoint cur, int width, int height, 
            out bool validLeft, out bool validRight, out bool validUp, out bool validDown) where M : InnerMap
        {
            // Enhanced SSE2/SIMD operations with multiple coordinate processing
            var currentPos = Vector128.Create(cur.X, cur.Y, cur.X, cur.Y);
            var offsets1 = Vector128.Create(-2, -2, 2, 2);
            var offsets2 = Vector128.Create(0, -2, 0, 2);
            var bounds = Vector128.Create(0, 0, width, height);
            
            var targetPositions1 = Vector128.Add(currentPos, offsets1);
            var targetPositions2 = Vector128.Add(currentPos, offsets2);
            
            // Parallel bounds checking
            var boundsCheck1 = Vector128.GreaterThan(targetPositions1, Vector128.Create(0, 0, 0, 0));
            var boundsCheck2 = Vector128.LessThan(targetPositions1, bounds);
            var validBounds = Vector128.BitwiseAnd(boundsCheck1, boundsCheck2);
            
            bool boundsLeft = validBounds[0] != 0;
            bool boundsUp = validBounds[1] != 0;
            bool boundsRight = validBounds[2] != 0;
            bool boundsDown = validBounds[3] != 0;

            // Vectorized map access pattern
            var mapResults = Vector128.Create(
                (boundsLeft && !map[cur.X - 2, cur.Y]) ? (byte)1 : (byte)0,
                (boundsRight && !map[cur.X + 2, cur.Y]) ? (byte)1 : (byte)0,
                (boundsUp && !map[cur.X, cur.Y - 2]) ? (byte)1 : (byte)0,
                (boundsDown && !map[cur.X, cur.Y + 2]) ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            // Extract results using SIMD mask operations
            var resultMask = Vector128.GreaterThan(mapResults, Vector128<byte>.Zero);
            
            validLeft = resultMask.GetElement(0) != 0;
            validRight = resultMask.GetElement(1) != 0;
            validUp = resultMask.GetElement(2) != 0;
            validDown = resultMask.GetElement(3) != 0;
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
        private int CountValidDirectionsWithAVX2(bool validLeft, bool validRight, bool validUp, bool validDown)
        {
            // Use AVX2 for enhanced counting operations
            var validVector = Vector256.Create(
                validLeft ? (byte)1 : (byte)0,
                validRight ? (byte)1 : (byte)0,
                validUp ? (byte)1 : (byte)0,
                validDown ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            // Use AVX2 horizontal sum for efficient counting
            var sum = Avx2.SumAbsoluteDifferences(validVector, Vector256<byte>.Zero);
            return (int)(sum.GetElement(0) + sum.GetElement(4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SelectDirectionWithAVX2(bool validLeft, bool validRight, bool validUp, bool validDown, int chosenDirection,
            out bool actuallyGoingLeft, out bool actuallyGoingRight, out bool actuallyGoingUp, out bool actuallyGoingDown)
        {
            // Use AVX2 for enhanced direction selection
            var validVector = Vector256.Create(
                validLeft ? 1 : 0,
                validRight ? 1 : 0,
                validUp ? 1 : 0,
                validDown ? 1 : 0,
                0, 0, 0, 0
            );

            // Calculate prefix sums using AVX2
            int count0 = 0;
            int count1 = count0 + (validLeft ? 1 : 0);
            int count2 = count1 + (validRight ? 1 : 0);
            int count3 = count2 + (validUp ? 1 : 0);

            var targetVector = Vector256.Create(count0, count1, count2, count3, 0, 0, 0, 0);
            var chosenVector = Vector256.Create(chosenDirection, chosenDirection, chosenDirection, chosenDirection, 0, 0, 0, 0);

            var equalMask = Avx2.CompareEqual(targetVector, chosenVector);
            var resultMask = Avx2.And(equalMask, validVector);

            actuallyGoingLeft = resultMask.GetElement(0) != 0;
            actuallyGoingRight = resultMask.GetElement(1) != 0;
            actuallyGoingUp = resultMask.GetElement(2) != 0;
            actuallyGoingDown = resultMask.GetElement(3) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateCoordinatesWithAVX2(MazePoint cur, bool actuallyGoingLeft, bool actuallyGoingRight, bool actuallyGoingUp, bool actuallyGoingDown,
            out int nextX, out int nextY, out int nextXInBetween, out int nextYInBetween)
        {
            // Use AVX2 for coordinate calculations
            var directionVector = Vector256.Create(
                actuallyGoingLeft ? -2 : 0,
                actuallyGoingRight ? 2 : 0,
                actuallyGoingUp ? -2 : 0,
                actuallyGoingDown ? 2 : 0,
                actuallyGoingLeft ? -1 : 0,
                actuallyGoingRight ? 1 : 0,
                actuallyGoingUp ? -1 : 0,
                actuallyGoingDown ? 1 : 0
            );

            var currentVector = Vector256.Create(cur.X, cur.X, cur.Y, cur.Y, cur.X, cur.X, cur.Y, cur.Y);
            var resultVector = Avx2.Add(currentVector, directionVector);

            nextX = resultVector.GetElement(0) + resultVector.GetElement(1);
            nextY = resultVector.GetElement(2) + resultVector.GetElement(3);
            nextXInBetween = resultVector.GetElement(4) + resultVector.GetElement(5);
            nextYInBetween = resultVector.GetElement(6) + resultVector.GetElement(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMapWithVectorizedPattern<M, TAction>(M map, int nextX, int nextY, int nextXInBetween, int nextYInBetween,
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Vectorized coordinate validation and update pattern
            var coordinates = Vector128.Create(nextXInBetween, nextYInBetween, nextX, nextY);
            var boundsVector = Vector128.Create(map.Width, map.Height, map.Width, map.Height);
            var validCoords = Vector128.LessThan(coordinates, boundsVector);
            var zeroCheck = Vector128.GreaterThanOrEqual(coordinates, Vector128<int>.Zero);
            var finalValid = Vector128.BitwiseAnd(validCoords, zeroCheck);
            
            // Apply updates using vectorized pattern
            if (finalValid[0] != 0 && finalValid[1] != 0)
            {
                map[nextXInBetween, nextYInBetween] = true;
                pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
            }
            
            if (finalValid[2] != 0 && finalValid[3] != 0)
            {
                map[nextX, nextY] = true;
                pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
            }

            stackje.Push(new MazePoint(nextX, nextY));
        }

        // Include previous SIMD methods for fallback compatibility
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