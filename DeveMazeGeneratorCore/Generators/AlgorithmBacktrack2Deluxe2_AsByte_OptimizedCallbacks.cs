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
    /// Optimized version using reduced callback frequency and batched callback invocations
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks : IAlgorithm<Maze>
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

            var stackje = new Stack<MazePoint>();
            stackje.Push(new MazePoint(1, 1));
            map[1, 1] = true;

            // Optimize callbacks - only call every N operations or use NoAction check
            const int CALLBACK_FREQUENCY = 100; // Call callback every 100 operations
            int operationCount = 0;
            bool isNoAction = typeof(TAction) == typeof(NoAction);

            if (!isNoAction)
            {
                pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);
            }

            while (stackje.Count != 0)
            {
                MazePoint cur = stackje.Peek();

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
                }
                else
                {
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

                    // Optimize callback frequency
                    if (!isNoAction && (++operationCount % CALLBACK_FREQUENCY == 0))
                    {
                        pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                        pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                    }
                }
            }

            return new Maze(map);
        }
    }
}