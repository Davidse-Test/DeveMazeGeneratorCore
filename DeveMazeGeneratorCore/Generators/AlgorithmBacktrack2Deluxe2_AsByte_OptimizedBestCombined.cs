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
    /// Optimized version combining the best individual optimizations: MazePointStructure + BranchPrediction + Inlining
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBestCombined : IAlgorithm<Maze>
    {
        public Maze GoGenerate<M, TAction>(int width, int height, int seed, IInnerMapFactory<M> mapFactory, IRandomFactory randomFactory, TAction pixelChangedCallback)
            where M : InnerMap
            where TAction : struct, IProgressAction
        {
            var innerMap = mapFactory.Create(width, height);
            var random = randomFactory.Create(seed);

            return GoGenerateInternal(innerMap, random, pixelChangedCallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private Maze GoGenerateInternal<M, TAction>(M map, IRandom random, TAction pixelChangedCallback) where M : InnerMap where TAction : struct, IProgressAction
        {
            long totSteps = (map.Width - 1L) / 2L * ((map.Height - 1L) / 2L);
            long currentStep = 1;

            int width = map.Width - 1;
            int height = map.Height - 1;

            // Use OptimizedMazePoint for better cache efficiency
            var stackje = new Stack<OptimizedMazePoint>();
            stackje.Push(new OptimizedMazePoint(1, 1));
            map[1, 1] = true;

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            while (stackje.Count != 0)
            {
                OptimizedMazePoint cur = stackje.Peek();

                // Reorder checks for better branch prediction - 
                // Right and Down are more likely in typical maze generation patterns
                bool validRight = cur.X + 2 < width && !map[cur.X + 2, cur.Y];
                bool validDown = cur.Y + 2 < height && !map[cur.X, cur.Y + 2];
                bool validLeft = cur.X - 2 > 0 && !map[cur.X - 2, cur.Y];
                bool validUp = cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2];

                // Convert to bytes in the same order for consistent behavior
                int validRightByte = validRight ? 1 : 0;
                int validDownByte = validDown ? 1 : 0;
                int validLeftByte = validLeft ? 1 : 0;
                int validUpByte = validUp ? 1 : 0;

                int targetCount = validRightByte + validDownByte + validLeftByte + validUpByte;

                // Most common case first for branch prediction
                if (targetCount != 0)
                {
                    var chosenDirection = random.Next(targetCount);
                    
                    var (nextX, nextY, nextXInBetween, nextYInBetween) = 
                        CalculateNextPosition(cur, chosenDirection, 
                                            validRight, validDown, validLeft, validUp,
                                            validRightByte, validDownByte, validLeftByte, validUpByte);

                    stackje.Push(new OptimizedMazePoint(nextX, nextY));
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
                else
                {
                    // Less common case - backtrack
                    stackje.Pop();
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int nextX, int nextY, int nextXInBetween, int nextYInBetween) 
            CalculateNextPosition(OptimizedMazePoint cur, int chosenDirection,
                                bool validRight, bool validDown, bool validLeft, bool validUp,
                                int validRightByte, int validDownByte, int validLeftByte, int validUpByte)
        {
            int countertje = 0;

            // Check directions in the same reordered manner for consistency
            bool actuallyGoingRight = validRight & chosenDirection == countertje;
            int actuallyGoingRightByte = actuallyGoingRight ? 1 : 0;
            countertje += validRightByte;

            bool actuallyGoingDown = validDown & chosenDirection == countertje;
            int actuallyGoingDownByte = actuallyGoingDown ? 1 : 0;
            countertje += validDownByte;

            bool actuallyGoingLeft = validLeft & chosenDirection == countertje;
            int actuallyGoingLeftByte = actuallyGoingLeft ? 1 : 0;
            countertje += validLeftByte;

            bool actuallyGoingUp = validUp & chosenDirection == countertje;
            int actuallyGoingUpByte = actuallyGoingUp ? 1 : 0;

            var nextX = cur.X + actuallyGoingLeftByte * -2 + actuallyGoingRightByte * 2;
            var nextY = cur.Y + actuallyGoingUpByte * -2 + actuallyGoingDownByte * 2;

            var nextXInBetween = cur.X - actuallyGoingLeftByte + actuallyGoingRightByte;
            var nextYInBetween = cur.Y - actuallyGoingUpByte + actuallyGoingDownByte;

            return (nextX, nextY, nextXInBetween, nextYInBetween);
        }
    }
}