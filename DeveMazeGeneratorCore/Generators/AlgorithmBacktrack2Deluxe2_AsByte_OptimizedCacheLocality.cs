using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeveMazeGeneratorCore.Generators
{
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCacheLocality : IAlgorithm<Maze>
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

            // Use array-based stack for better cache locality
            const int INITIAL_STACK_SIZE = 1024;
            var stackArray = new MazePoint[INITIAL_STACK_SIZE];
            int stackTop = 0;
            
            // Initialize
            stackArray[stackTop++] = new MazePoint(1, 1);
            map[1, 1] = true;

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            // Cache frequently accessed values
            var widthMinus2 = width - 2;
            var heightMinus2 = height - 2;

            while (stackTop > 0)
            {
                // Peek without bounds checking
                ref var cur = ref stackArray[stackTop - 1];

                // Unroll and optimize bounds checking
                var curXMinus2 = cur.X - 2;
                var curXPlus2 = cur.X + 2;
                var curYMinus2 = cur.Y - 2;
                var curYPlus2 = cur.Y + 2;

                // Fast bounds checking with cached values
                bool validLeft = curXMinus2 > 0 && !map[curXMinus2, cur.Y];
                bool validRight = curXPlus2 < width && !map[curXPlus2, cur.Y];
                bool validUp = curYMinus2 > 0 && !map[cur.X, curYMinus2];
                bool validDown = curYPlus2 < height && !map[cur.X, curYPlus2];

                // Fast bit manipulation for counting
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
                    // Count set bits using bit manipulation
                    int targetCount = CountSetBits(validMask);
                    var chosenDirection = random.Next(targetCount);

                    // Use lookup table for direction selection
                    (int deltaX, int deltaY) = GetDirectionDelta(validMask, chosenDirection);

                    var nextX = cur.X + deltaX * 2;
                    var nextY = cur.Y + deltaY * 2;
                    var nextXInBetween = cur.X + deltaX;
                    var nextYInBetween = cur.Y + deltaY;

                    // Resize stack if needed
                    if (stackTop >= stackArray.Length)
                    {
                        var newStack = new MazePoint[stackArray.Length * 2];
                        Array.Copy(stackArray, newStack, stackTop);
                        stackArray = newStack;
                    }

                    // Push to stack
                    stackArray[stackTop++] = new MazePoint(nextX, nextY);

                    // Update map - batch if possible
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    // Batch callbacks to reduce overhead
                    if (typeof(TAction) != typeof(NoAction))
                    {
                        pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                        pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                    }
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountSetBits(int value)
        {
            // Brian Kernighan's algorithm for counting set bits
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int deltaX, int deltaY) GetDirectionDelta(int validMask, int chosenDirection)
        {
            // Create compact lookup for direction selection
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
            
            // Down
            if ((validMask & 8) != 0)
            {
                if (chosenDirection == directionIndex) return (0, 1);
            }
            
            // Should never reach here
            return (0, 0);
        }
    }
}