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
    /// <summary>
    /// Optimized version with loop unrolling and reduced branching for better CPU performance
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedLoopUnrolling : IAlgorithm<Maze>
    {
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

                // Manually unroll direction checking with direct comparisons
                int curX = cur.X;
                int curY = cur.Y;
                
                // Unrolled direction checks with single memory access per direction
                bool validLeft = curX > 2 && !map[curX - 2, curY];
                bool validRight = curX + 2 < width && !map[curX + 2, curY];
                bool validUp = curY > 2 && !map[curX, curY - 2];
                bool validDown = curY + 2 < height && !map[curX, curY + 2];

                // Manual bit manipulation instead of Unsafe.As
                int validLeftByte = validLeft ? 1 : 0;
                int validRightByte = validRight ? 1 : 0;
                int validUpByte = validUp ? 1 : 0;
                int validDownByte = validDown ? 1 : 0;

                int targetCount = validLeftByte + validRightByte + validUpByte + validDownByte;

                if (targetCount == 0)
                {
                    stackje.Pop();
                }
                else
                {
                    var chosenDirection = random.Next(targetCount);
                    
                    // Unrolled direction selection with reduced branching
                    int actuallyGoingLeftByte = 0;
                    int actuallyGoingRightByte = 0;
                    int actuallyGoingUpByte = 0;
                    int actuallyGoingDownByte = 0;
                    
                    int countertje = 0;
                    
                    // Left direction
                    if (validLeft)
                    {
                        if (chosenDirection == countertje)
                            actuallyGoingLeftByte = 1;
                        countertje++;
                    }
                    
                    // Right direction  
                    if (validRight)
                    {
                        if (chosenDirection == countertje)
                            actuallyGoingRightByte = 1;
                        countertje++;
                    }
                    
                    // Up direction
                    if (validUp)
                    {
                        if (chosenDirection == countertje)
                            actuallyGoingUpByte = 1;
                        countertje++;
                    }
                    
                    // Down direction
                    if (validDown)
                    {
                        if (chosenDirection == countertje)
                            actuallyGoingDownByte = 1;
                    }

                    // Direct arithmetic without multiplication where possible
                    int nextX = curX;
                    int nextY = curY;
                    
                    if (actuallyGoingLeftByte != 0)
                    {
                        nextX -= 2;
                    }
                    else if (actuallyGoingRightByte != 0)
                    {
                        nextX += 2;
                    }
                    
                    if (actuallyGoingUpByte != 0)
                    {
                        nextY -= 2;
                    }
                    else if (actuallyGoingDownByte != 0)
                    {
                        nextY += 2;
                    }

                    int nextXInBetween = curX;
                    int nextYInBetween = curY;
                    
                    if (actuallyGoingLeftByte != 0)
                    {
                        nextXInBetween--;
                    }
                    else if (actuallyGoingRightByte != 0)
                    {
                        nextXInBetween++;
                    }
                    
                    if (actuallyGoingUpByte != 0)
                    {
                        nextYInBetween--;
                    }
                    else if (actuallyGoingDownByte != 0)
                    {
                        nextYInBetween++;
                    }

                    stackje.Push(new MazePoint(nextX, nextY));
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }
    }
}