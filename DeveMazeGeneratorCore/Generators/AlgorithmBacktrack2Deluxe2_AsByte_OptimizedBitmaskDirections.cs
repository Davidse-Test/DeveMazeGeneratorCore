using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeveMazeGeneratorCore.Generators
{
    /// <summary>
    /// Optimized version using bitmask operations for direction selection to reduce branching
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBitmaskDirections : IAlgorithm<Maze>
    {
        // Direction constants as bit flags
        private const int DIR_LEFT = 1;
        private const int DIR_RIGHT = 2;
        private const int DIR_UP = 4;
        private const int DIR_DOWN = 8;

        public Maze GoGenerate<M, TAction>(int width, int height, int seed, IInnerMapFactory<M> mapFactory, IRandomFactory randomFactory, TAction pixelChangedCallback)
            where M : InnerMap
            where TAction : struct, IProgressAction
        {
            var innerMap = mapFactory.Create(width, height);
            var random = randomFactory.Create(seed);

            return GoGenerateInternal(innerMap, random, pixelChangedCallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

                // Build bitmask of valid directions
                int validDirections = 0;
                
                if (cur.X - 2 > 0 && !map[cur.X - 2, cur.Y])
                    validDirections |= DIR_LEFT;
                    
                if (cur.X + 2 < width && !map[cur.X + 2, cur.Y])
                    validDirections |= DIR_RIGHT;
                    
                if (cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2])
                    validDirections |= DIR_UP;
                    
                if (cur.Y + 2 < height && !map[cur.X, cur.Y + 2])
                    validDirections |= DIR_DOWN;

                if (validDirections == 0)
                {
                    stackje.Pop();
                }
                else
                {
                    // Count valid directions using bit counting
                    int targetCount = PopCount(validDirections);
                    var chosenDirection = random.Next(targetCount);
                    
                    // Select the chosen direction using bit manipulation
                    int selectedDirection = SelectNthSetBit(validDirections, chosenDirection);
                    
                    // Calculate movement based on selected direction
                    int deltaX = 0, deltaY = 0;
                    int deltaXInBetween = 0, deltaYInBetween = 0;
                    
                    switch (selectedDirection)
                    {
                        case DIR_LEFT:
                            deltaX = -2;
                            deltaXInBetween = -1;
                            break;
                        case DIR_RIGHT:
                            deltaX = 2;
                            deltaXInBetween = 1;
                            break;
                        case DIR_UP:
                            deltaY = -2;
                            deltaYInBetween = -1;
                            break;
                        case DIR_DOWN:
                            deltaY = 2;
                            deltaYInBetween = 1;
                            break;
                    }

                    var nextX = cur.X + deltaX;
                    var nextY = cur.Y + deltaY;
                    var nextXInBetween = cur.X + deltaXInBetween;
                    var nextYInBetween = cur.Y + deltaYInBetween;

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
        private static int PopCount(int value)
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
        private static int SelectNthSetBit(int value, int n)
        {
            // Find the nth set bit (0-indexed)
            int count = 0;
            int bit = 1;
            
            while (bit <= value)
            {
                if ((value & bit) != 0)
                {
                    if (count == n)
                        return bit;
                    count++;
                }
                bit <<= 1;
            }
            
            return 0; // Should never reach here with valid input
        }
    }
}