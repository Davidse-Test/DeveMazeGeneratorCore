using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeveMazeGeneratorCore.Generators
{
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDMemoryCombined : IAlgorithm<Maze>
    {
        public Maze GoGenerate<M, TAction>(int width, int height, int seed, IInnerMapFactory<M> mapFactory, IRandomFactory randomFactory, TAction pixelChangedCallback)
            where M : InnerMap
            where TAction : struct, IProgressAction
        {
            // Try to use optimized memory layout, fallback to regular factory
            var innerMap = new BitArreintjeFastInnerMapOptimizedMemoryLayout(width, height) as M ??
                          mapFactory.Create(width, height);
            
            var random = randomFactory.Create(seed);

            return GoGenerateInternal(innerMap, random, pixelChangedCallback);
        }

        private Maze GoGenerateInternal<M, TAction>(M map, IRandom random, TAction pixelChangedCallback) where M : InnerMap where TAction : struct, IProgressAction
        {
            long totSteps = (map.Width - 1L) / 2L * ((map.Height - 1L) / 2L);
            long currentStep = 1;

            int width = map.Width - 1;
            int height = map.Height - 1;

            // Use ArrayPool for better allocation performance
            var stackPool = ArrayPool<MazePoint>.Shared;
            var stackArray = stackPool.Rent(1024);
            int stackTop = 0;
            
            try
            {
                // Initialize
                stackArray[stackTop++] = new MazePoint(1, 1);
                map[1, 1] = true;

                pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

                // Cache frequently accessed values for branch prediction
                var widthMinus2 = width - 2;
                var heightMinus2 = height - 2;

                while (stackTop > 0)
                {
                    // Peek optimization - direct array access
                    ref var cur = ref stackArray[stackTop - 1];

                    // Precompute coordinates to help with CPU pipeline
                    var curXMinus2 = cur.X - 2;
                    var curXPlus2 = cur.X + 2;
                    var curYMinus2 = cur.Y - 2;
                    var curYPlus2 = cur.Y + 2;

                    // Fast bounds checking with cached values
                    bool validLeft = curXMinus2 > 0 && !map[curXMinus2, cur.Y];
                    bool validRight = curXPlus2 < width && !map[curXPlus2, cur.Y];
                    bool validUp = curYMinus2 > 0 && !map[cur.X, curYMinus2];
                    bool validDown = curYPlus2 < height && !map[cur.X, curYPlus2];

                    // Use bit manipulation for fast counting and selection
                    int validMask = (validLeft ? 1 : 0) | 
                                   (validRight ? 2 : 0) | 
                                   (validUp ? 4 : 0) | 
                                   (validDown ? 8 : 0);

                    if (validMask == 0)
                    {
                        stackTop--; // Pop
                    }
                    else
                    {
                        // Count set bits efficiently
                        int targetCount = PopCount(validMask);
                        var chosenDirection = random.Next(targetCount);

                        // Use lookup table for direction selection
                        (int deltaX, int deltaY) = GetDirectionDeltaFast(validMask, chosenDirection);

                        var nextX = cur.X + deltaX * 2;
                        var nextY = cur.Y + deltaY * 2;
                        var nextXInBetween = cur.X + deltaX;
                        var nextYInBetween = cur.Y + deltaY;

                        // Resize stack if needed using ArrayPool
                        if (stackTop >= stackArray.Length)
                        {
                            var newSize = stackArray.Length * 2;
                            var newStack = stackPool.Rent(newSize);
                            Array.Copy(stackArray, newStack, stackTop);
                            stackPool.Return(stackArray);
                            stackArray = newStack;
                        }

                        // Push to stack
                        stackArray[stackTop++] = new MazePoint(nextX, nextY);

                        // Update map - batch access
                        map[nextXInBetween, nextYInBetween] = true;
                        map[nextX, nextY] = true;

                        // Optimize callbacks for NoAction case
                        if (typeof(TAction) != typeof(NoAction))
                        {
                            pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                            pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                        }
                    }
                }
            }
            finally
            {
                // Always return to pool
                stackPool.Return(stackArray);
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopCount(int value)
        {
            // Use the most efficient bit counting available
            if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
            {
                return (int)System.Runtime.Intrinsics.X86.Popcnt.PopCount((uint)value);
            }
            
            // Fallback to Brian Kernighan's algorithm
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int deltaX, int deltaY) GetDirectionDeltaFast(int validMask, int chosenDirection)
        {
            // Use bit scanning for faster direction selection
            int directionIndex = 0;
            
            // Left
            if ((validMask & 1) != 0)
            {
                if (chosenDirection == directionIndex) return (-1, 0);
                directionIndex++;
            }
            
            // Right  
            if ((validMask & 2) != 0)
            {
                if (chosenDirection == directionIndex) return (1, 0);
                directionIndex++;
            }
            
            // Up
            if ((validMask & 4) != 0)
            {
                if (chosenDirection == directionIndex) return (0, -1);
                directionIndex++;
            }
            
            // Down (must be this if we get here)
            return (0, 1);
        }
    }
}