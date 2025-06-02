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
    /// Optimized version with aggressively inlined methods and reduced method call overhead
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedInlining : IAlgorithm<Maze>
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

            var stackje = new Stack<MazePoint>();
            stackje.Push(new MazePoint(1, 1));
            map[1, 1] = true;

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            while (stackje.Count != 0)
            {
                MazePoint cur = stackje.Peek();

                // Inline the direction checking logic completely
                var (hasValidDirection, nextX, nextY, nextXInBetween, nextYInBetween) = 
                    CheckDirectionsAndCalculateNext(cur, map, width, height, random);

                if (hasValidDirection)
                {
                    stackje.Push(new MazePoint(nextX, nextY));
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
                else
                {
                    stackje.Pop();
                }
            }

            return new Maze(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool hasValidDirection, int nextX, int nextY, int nextXInBetween, int nextYInBetween) 
            CheckDirectionsAndCalculateNext<M>(MazePoint cur, M map, int width, int height, IRandom random) where M : InnerMap
        {
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
                return (false, 0, 0, 0, 0);
            }

            var chosenDirection = random.Next(targetCount);
            var (actuallyGoingLeftByte, actuallyGoingRightByte, actuallyGoingUpByte, actuallyGoingDownByte) = 
                CalculateDirectionBytes(validLeft, validRight, validUp, validDown, 
                                      validLeftByte, validRightByte, validUpByte, validDownByte, 
                                      chosenDirection);

            var nextX = cur.X + actuallyGoingLeftByte * -2 + actuallyGoingRightByte * 2;
            var nextY = cur.Y + actuallyGoingUpByte * -2 + actuallyGoingDownByte * 2;

            var nextXInBetween = cur.X - actuallyGoingLeftByte + actuallyGoingRightByte;
            var nextYInBetween = cur.Y - actuallyGoingUpByte + actuallyGoingDownByte;

            return (true, nextX, nextY, nextXInBetween, nextYInBetween);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (byte actuallyGoingLeftByte, byte actuallyGoingRightByte, byte actuallyGoingUpByte, byte actuallyGoingDownByte)
            CalculateDirectionBytes(bool validLeft, bool validRight, bool validUp, bool validDown,
                                  int validLeftByte, int validRightByte, int validUpByte, int validDownByte,
                                  int chosenDirection)
        {
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

            return (actuallyGoingLeftByte, actuallyGoingRightByte, actuallyGoingUpByte, actuallyGoingDownByte);
        }
    }
}