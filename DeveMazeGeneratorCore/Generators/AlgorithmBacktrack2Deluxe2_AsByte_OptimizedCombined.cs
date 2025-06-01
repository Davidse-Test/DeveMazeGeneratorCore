using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DeveMazeGeneratorCore.Generators
{
    /// <summary>
    /// Combined optimized version incorporating multiple performance improvements:
    /// - ArrayPool for stack allocation
    /// - Packed coordinates structure
    /// - Batched random generation
    /// - Optimized callbacks
    /// - Lookup tables for directions
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombined : IAlgorithm<Maze>
    {
        // Pre-calculated lookup tables for direction offsets
        private static readonly int[] DxOffsets = { -2, 2, 0, 0 };  // Left, Right, Up, Down
        private static readonly int[] DyOffsets = { 0, 0, -2, 2 };  // Left, Right, Up, Down
        private static readonly int[] DxInBetween = { -1, 1, 0, 0 }; // Left, Right, Up, Down
        private static readonly int[] DyInBetween = { 0, 0, -1, 1 }; // Left, Right, Up, Down

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

            // Use ArrayPool for better memory management
            int maxStackSize = Math.Max(8192, width * height / 8);
            var stackArray = ArrayPool<OptimizedMazePoint>.Shared.Rent(maxStackSize);
            int stackTop = -1;

            // Batched random generation
            uint randomBatch = 0;
            int bitsUsed = 32;

            // Callback optimization
            const int CALLBACK_FREQUENCY = 100;
            int operationCount = 0;
            bool isNoAction = typeof(TAction) == typeof(NoAction);

            try
            {
                // Push initial point
                stackArray[++stackTop] = new OptimizedMazePoint(1, 1);
                map[1, 1] = true;

                if (!isNoAction)
                {
                    pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);
                }

                while (stackTop >= 0)
                {
                    OptimizedMazePoint cur = stackArray[stackTop];

                    // Use bit-packed direction checking
                    byte validDirections = 0;
                    if (cur.X - 2 > 0 && !map[cur.X - 2, cur.Y]) validDirections |= 1; // Left
                    if (cur.X + 2 < width && !map[cur.X + 2, cur.Y]) validDirections |= 2; // Right
                    if (cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2]) validDirections |= 4; // Up
                    if (cur.Y + 2 < height && !map[cur.X, cur.Y + 2]) validDirections |= 8; // Down

                    int targetCount = CountBits(validDirections);

                    if (targetCount == 0)
                    {
                        stackTop--; // Pop from array-based stack
                    }
                    else
                    {
                        // Use batched random generation
                        int chosenDirection = GetNextRandom(random, ref randomBatch, ref bitsUsed, targetCount);
                        int directionIndex = GetDirectionIndex(validDirections, chosenDirection);
                        
                        var nextX = cur.X + DxOffsets[directionIndex];
                        var nextY = cur.Y + DyOffsets[directionIndex];
                        var nextXInBetween = cur.X + DxInBetween[directionIndex];
                        var nextYInBetween = cur.Y + DyInBetween[directionIndex];

                        // Push to array-based stack with bounds checking
                        if (stackTop >= stackArray.Length - 1)
                        {
                            throw new InvalidOperationException($"Stack overflow - maze too complex for array-based stack. Stack size: {stackArray.Length}, requested: {stackTop + 1}");
                        }
                        
                        stackArray[++stackTop] = new OptimizedMazePoint(nextX, nextY);
                        map[nextXInBetween, nextYInBetween] = true;
                        map[nextX, nextY] = true;

                        // Optimized callback frequency
                        if (!isNoAction && (++operationCount % CALLBACK_FREQUENCY == 0))
                        {
                            pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                            pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<OptimizedMazePoint>.Shared.Return(stackArray, clearArray: false);
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountBits(byte value)
        {
            value = (byte)(value - ((value >> 1) & 0x55));
            value = (byte)((value & 0x33) + ((value >> 2) & 0x33));
            return (value + (value >> 4)) & 0x0F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDirectionIndex(byte validDirections, int chosenDirection)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if ((validDirections & (1 << i)) != 0)
                {
                    if (count == chosenDirection)
                        return i;
                    count++;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNextRandom(IRandom random, ref uint randomBatch, ref int bitsUsed, int maxValue)
        {
            if (maxValue <= 4)
            {
                if (bitsUsed >= 30)
                {
                    randomBatch = (uint)random.Next();
                    bitsUsed = 0;
                }

                int result = (int)(randomBatch >> bitsUsed) & 0x3;
                bitsUsed += 2;
                
                while (result >= maxValue)
                {
                    if (bitsUsed >= 30)
                    {
                        randomBatch = (uint)random.Next();
                        bitsUsed = 0;
                    }
                    result = (int)(randomBatch >> bitsUsed) & 0x3;
                    bitsUsed += 2;
                }
                
                return result;
            }
            else
            {
                return random.Next(maxValue);
            }
        }
    }
}