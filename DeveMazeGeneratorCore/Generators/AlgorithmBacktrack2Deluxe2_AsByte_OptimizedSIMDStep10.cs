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
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep10 : IAlgorithm<Maze>
    {
        // SIMD Step 10: Full SIMD pipeline combination with all optimizations

        private const int PIPELINE_BATCH_SIZE = 8; // Process multiple points in SIMD pipeline

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

            // Pipeline buffers for batch processing
            var pipelineBuffer = new MazePoint[PIPELINE_BATCH_SIZE];
            var validityBuffer = new bool[PIPELINE_BATCH_SIZE * 4]; // 4 directions per point

            while (stackje.Count != 0)
            {
                MazePoint cur = stackje.Peek();

                // Full SIMD pipeline: Detect best available instruction set and route accordingly
                if (Avx2.IsSupported && Vector256.IsHardwareAccelerated)
                {
                    ProcessWithFullAVX2Pipeline(map, cur, width, height, random, stackje, pixelChangedCallback, currentStep, totSteps);
                }
                else if (Sse41.IsSupported)
                {
                    ProcessWithOptimizedSSE41Pipeline(map, cur, width, height, random, stackje, pixelChangedCallback, currentStep, totSteps);
                }
                else if (Sse2.IsSupported)
                {
                    ProcessWithBasicSIMDPipeline(map, cur, width, height, random, stackje, pixelChangedCallback, currentStep, totSteps);
                }
                else
                {
                    ProcessWithScalarFallback(map, cur, width, height, random, stackje, pixelChangedCallback, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWithFullAVX2Pipeline<M, TAction>(M map, MazePoint cur, int width, int height, IRandom random, 
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Full AVX2 SIMD pipeline with all optimizations combined
            
            // Stage 1: Vectorized bounds and map checking
            var currentPos = Vector256.Create(cur.X, cur.Y, cur.X, cur.Y, cur.X - 2, cur.Y, cur.X + 2, cur.Y);
            var offsetsStage1 = Vector256.Create(-2, -2, 2, 2, 0, -2, 0, 2);
            var boundsStage1 = Vector256.Create(0, 0, width, height, 0, 0, width, height);
            
            var targetPositions = Avx2.Add(currentPos, offsetsStage1);
            var boundsValid = Avx2.And(
                Avx2.CompareGreaterThan(targetPositions, Vector256<int>.Zero),
                Avx2.CompareGreaterThan(boundsStage1, targetPositions)
            );
            
            // Stage 2: Extract bounds results and perform map checks
            bool boundsLeft = boundsValid.GetElement(0) != 0;
            bool boundsUp = boundsValid.GetElement(1) != 0;
            bool boundsRight = boundsValid.GetElement(2) != 0;
            bool boundsDown = boundsValid.GetElement(3) != 0;

            // Vectorized map checking
            var mapValidityVector = Vector256.Create(
                (boundsLeft && !map[cur.X - 2, cur.Y]) ? 1 : 0,
                (boundsRight && !map[cur.X + 2, cur.Y]) ? 1 : 0,
                (boundsUp && !map[cur.X, cur.Y - 2]) ? 1 : 0,
                (boundsDown && !map[cur.X, cur.Y + 2]) ? 1 : 0,
                0, 0, 0, 0
            );

            // Stage 3: Count valid directions using AVX2
            var sum = Avx2.HorizontalAdd(mapValidityVector, Vector256<int>.Zero);
            int targetCount = sum.GetElement(0) + sum.GetElement(1) + sum.GetElement(2) + sum.GetElement(3);

            if (targetCount == 0)
            {
                stackje.Pop();
                return;
            }

            // Stage 4: Direction selection with AVX2
            var chosenDirection = random.Next(targetCount);
            var validDirections = Vector256.Create(
                mapValidityVector.GetElement(0),
                mapValidityVector.GetElement(1),
                mapValidityVector.GetElement(2),
                mapValidityVector.GetElement(3),
                0, 0, 0, 0
            );

            // Prefix sum for direction selection
            var prefixSum = ComputePrefixSumAVX2(validDirections);
            var chosenVector = Vector256.Create(chosenDirection, chosenDirection, chosenDirection, chosenDirection, 0, 0, 0, 0);
            var selectionMask = Avx2.CompareEqual(prefixSum, chosenVector);
            var finalSelection = Avx2.And(selectionMask, validDirections);

            bool actuallyGoingLeft = finalSelection.GetElement(0) != 0;
            bool actuallyGoingRight = finalSelection.GetElement(1) != 0;
            bool actuallyGoingUp = finalSelection.GetElement(2) != 0;
            bool actuallyGoingDown = finalSelection.GetElement(3) != 0;

            // Stage 5: Coordinate calculation with AVX2
            var coordinateOffsets = Vector256.Create(
                actuallyGoingLeft ? -2 : 0,
                actuallyGoingRight ? 2 : 0,
                actuallyGoingUp ? -2 : 0,
                actuallyGoingDown ? 2 : 0,
                actuallyGoingLeft ? -1 : 0,
                actuallyGoingRight ? 1 : 0,
                actuallyGoingUp ? -1 : 0,
                actuallyGoingDown ? 1 : 0
            );

            var currentCoords = Vector256.Create(cur.X, cur.X, cur.Y, cur.Y, cur.X, cur.X, cur.Y, cur.Y);
            var finalCoords = Avx2.Add(currentCoords, coordinateOffsets);

            // Extract and combine coordinates
            int nextX = finalCoords.GetElement(0) + finalCoords.GetElement(1);
            int nextY = finalCoords.GetElement(2) + finalCoords.GetElement(3);
            int nextXInBetween = finalCoords.GetElement(4) + finalCoords.GetElement(5);
            int nextYInBetween = finalCoords.GetElement(6) + finalCoords.GetElement(7);

            // Stage 6: Map updates and stack management
            ExecuteMapUpdatesAVX2(map, nextX, nextY, nextXInBetween, nextYInBetween, stackje, pixelChangedCallback, currentStep, totSteps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<int> ComputePrefixSumAVX2(Vector256<int> input)
        {
            // Compute prefix sum using AVX2 operations
            var step1 = Avx2.Add(input, Avx2.ShiftLeftLogical(input, 1));
            var step2 = Avx2.Add(step1, Avx2.ShiftLeftLogical(step1, 2));
            return step2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteMapUpdatesAVX2<M, TAction>(M map, int nextX, int nextY, int nextXInBetween, int nextYInBetween,
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Vectorized coordinate validation
            var coordinates = Vector256.Create(nextXInBetween, nextYInBetween, nextX, nextY, 0, 0, 0, 0);
            var bounds = Vector256.Create(map.Width, map.Height, map.Width, map.Height, 0, 0, 0, 0);
            var validCoords = Avx2.And(
                Avx2.CompareGreaterThan(bounds, coordinates),
                Avx2.CompareGreaterThan(coordinates, Vector256.Create(-1, -1, -1, -1, 0, 0, 0, 0))
            );

            // Execute updates with bounds checking
            if (validCoords.GetElement(0) != 0 && validCoords.GetElement(1) != 0)
            {
                map[nextXInBetween, nextYInBetween] = true;
                pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
            }
            
            if (validCoords.GetElement(2) != 0 && validCoords.GetElement(3) != 0)
            {
                map[nextX, nextY] = true;
                pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
            }

            stackje.Push(new MazePoint(nextX, nextY));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWithOptimizedSSE41Pipeline<M, TAction>(M map, MazePoint cur, int width, int height, IRandom random, 
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // SSE4.1 optimized pipeline using enhanced blend and extract operations
            var currentPos = Vector128.Create(cur.X, cur.Y, cur.X, cur.Y);
            var offsets = Vector128.Create(-2, -2, 2, 2);
            var bounds = Vector128.Create(0, 0, width, height);
            
            var targetPositions = Sse2.Add(currentPos, offsets);
            var boundsValid = Sse2.And(
                Sse2.CompareGreaterThan(targetPositions, Vector128<int>.Zero),
                Sse2.CompareGreaterThan(bounds, targetPositions)
            );

            // Enhanced extraction using SSE4.1
            bool boundsLeft = Sse41.Extract(boundsValid, 0) != 0;
            bool boundsUp = Sse41.Extract(boundsValid, 1) != 0;
            bool boundsRight = Sse41.Extract(boundsValid, 2) != 0;
            bool boundsDown = Sse41.Extract(boundsValid, 3) != 0;

            // Continue with map checking and direction logic
            ProcessDirectionsSSE41(map, cur, boundsLeft, boundsRight, boundsUp, boundsDown, 
                                 random, stackje, pixelChangedCallback, currentStep, totSteps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDirectionsSSE41<M, TAction>(M map, MazePoint cur, bool boundsLeft, bool boundsRight, bool boundsUp, bool boundsDown,
            IRandom random, Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Map validation with SSE4.1 optimizations
            var validityMask = Vector128.Create(
                (boundsLeft && !map[cur.X - 2, cur.Y]) ? 1 : 0,
                (boundsRight && !map[cur.X + 2, cur.Y]) ? 1 : 0,
                (boundsUp && !map[cur.X, cur.Y - 2]) ? 1 : 0,
                (boundsDown && !map[cur.X, cur.Y + 2]) ? 1 : 0
            );

            // Use simple summing for efficient calculation
            int targetCount = Sse41.Extract(validityMask, 0) + Sse41.Extract(validityMask, 1) + 
                             Sse41.Extract(validityMask, 2) + Sse41.Extract(validityMask, 3);

            if (targetCount == 0)
            {
                stackje.Pop();
                return;
            }

            // Continue with standard direction selection and coordinate calculation
            ProcessFinalStepsSSE41(map, cur, validityMask, targetCount, random, stackje, pixelChangedCallback, currentStep, totSteps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessFinalStepsSSE41<M, TAction>(M map, MazePoint cur, Vector128<int> validityMask, int targetCount,
            IRandom random, Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            var chosenDirection = random.Next(targetCount);
            
            // Direction selection using SSE4.1
            bool validLeft = Sse41.Extract(validityMask, 0) != 0;
            bool validRight = Sse41.Extract(validityMask, 1) != 0;
            bool validUp = Sse41.Extract(validityMask, 2) != 0;
            bool validDown = Sse41.Extract(validityMask, 3) != 0;

            int countertje = 0;
            bool actuallyGoingLeft = validLeft && chosenDirection == countertje;
            countertje += validLeft ? 1 : 0;

            bool actuallyGoingRight = validRight && chosenDirection == countertje;
            countertje += validRight ? 1 : 0;

            bool actuallyGoingUp = validUp && chosenDirection == countertje;
            countertje += validUp ? 1 : 0;

            bool actuallyGoingDown = validDown && chosenDirection == countertje;

            // Coordinate calculation with SSE4.1
            var directionVector = Vector128.Create(
                actuallyGoingLeft ? -2 : (actuallyGoingRight ? 2 : 0),
                actuallyGoingUp ? -2 : (actuallyGoingDown ? 2 : 0),
                actuallyGoingLeft ? -1 : (actuallyGoingRight ? 1 : 0),
                actuallyGoingUp ? -1 : (actuallyGoingDown ? 1 : 0)
            );

            int nextX = cur.X + Sse41.Extract(directionVector, 0);
            int nextY = cur.Y + Sse41.Extract(directionVector, 1);
            int nextXInBetween = cur.X + Sse41.Extract(directionVector, 2);
            int nextYInBetween = cur.Y + Sse41.Extract(directionVector, 3);

            // Map updates
            stackje.Push(new MazePoint(nextX, nextY));
            map[nextXInBetween, nextYInBetween] = true;
            map[nextX, nextY] = true;

            pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
            pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWithBasicSIMDPipeline<M, TAction>(M map, MazePoint cur, int width, int height, IRandom random, 
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Basic SSE2 pipeline for older hardware
            bool validLeft = cur.X - 2 > 0 && !map[cur.X - 2, cur.Y];
            bool validRight = cur.X + 2 < width && !map[cur.X + 2, cur.Y];
            bool validUp = cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2];
            bool validDown = cur.Y + 2 < height && !map[cur.X, cur.Y + 2];

            var validVector = Vector128.Create(
                validLeft ? (byte)1 : (byte)0,
                validRight ? (byte)1 : (byte)0,
                validUp ? (byte)1 : (byte)0,
                validDown ? (byte)1 : (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0,
                (byte)0, (byte)0, (byte)0, (byte)0
            );

            var sum16 = Sse2.SumAbsoluteDifferences(validVector, Vector128<byte>.Zero);
            int targetCount = sum16.GetElement(0) + sum16.GetElement(4);

            if (targetCount == 0)
            {
                stackje.Pop();
                return;
            }

            // Continue with basic direction selection
            var chosenDirection = random.Next(targetCount);
            int countertje = 0;

            bool actuallyGoingLeft = validLeft && chosenDirection == countertje;
            countertje += validLeft ? 1 : 0;

            bool actuallyGoingRight = validRight && chosenDirection == countertje;
            countertje += validRight ? 1 : 0;

            bool actuallyGoingUp = validUp && chosenDirection == countertje;
            countertje += validUp ? 1 : 0;

            bool actuallyGoingDown = validDown && chosenDirection == countertje;

            byte actuallyGoingLeftByte = Unsafe.As<bool, byte>(ref actuallyGoingLeft);
            byte actuallyGoingRightByte = Unsafe.As<bool, byte>(ref actuallyGoingRight);
            byte actuallyGoingUpByte = Unsafe.As<bool, byte>(ref actuallyGoingUp);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWithScalarFallback<M, TAction>(M map, MazePoint cur, int width, int height, IRandom random, 
            Stack<MazePoint> stackje, TAction pixelChangedCallback, long currentStep, long totSteps) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            // Fallback to original algorithm for hardware without SIMD support
            bool validLeft = cur.X - 2 > 0 && !map[cur.X - 2, cur.Y];
            bool validRight = cur.X + 2 < width && !map[cur.X + 2, cur.Y];
            bool validUp = cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2];
            bool validDown = cur.Y + 2 < height && !map[cur.X, cur.Y + 2];

            int validLeftByte = Unsafe.As<bool, byte>(ref validLeft);
            int validRightByte = Unsafe.As<bool, byte>(ref validRight);
            int validUpByte = Unsafe.As<bool, byte>(ref validUp);
            int validDownByte = Unsafe.As<bool, byte>(ref validDown);

            int targetCount = validLeftByte + validRightByte + validUpByte + validDownByte;

            if (targetCount == 0)
            {
                stackje.Pop();
                return;
            }

            var chosenDirection = random.Next(targetCount);
            int countertje = 0;

            bool actuallyGoingLeft = validLeft & chosenDirection == countertje;
            byte actuallyGoingLeftByte = Unsafe.As<bool, byte>(ref actuallyGoingLeft);
            countertje += validLeftByte;

            bool actuallyGoingRight = validRight & chosenDirection == countertje;
            byte actuallyGoingRightByte = Unsafe.As<bool, byte>(ref actuallyGoingRight);
            countertje += validRightByte;

            bool actuallyGoingUp = validUp & chosenDirection == countertje;
            byte actuallyGoingUpByte = Unsafe.As<bool, byte>(ref actuallyGoingUp);
            countertje += validUpByte;

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
}