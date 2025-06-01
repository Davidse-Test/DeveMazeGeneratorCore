using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using DeveMazeGeneratorCore.Structures;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeveMazeGeneratorCore.Generators
{
    public class AlgorithmBacktrack2Deluxe2_AsByte : IAlgorithm<Maze>
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
            
            // Pre-calculate bounds to avoid repeated comparisons
            int xMin = 2;
            int xMax = width;
            int yMin = 2; 
            int yMax = height;

            // Use ArrayPool for better memory management
            int maxStackSize = (int)totSteps; // Worst case: all cells on the stack
            var stackArray = ArrayPool<MazePoint>.Shared.Rent(maxStackSize);
            int stackIndex = 0;

            try
            {
                // Push initial point
                stackArray[stackIndex++] = new MazePoint(1, 1);
                map[1, 1] = true;

                pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

                while (stackIndex > 0)
                {
                    // Peek (get last element without removing)
                    MazePoint cur = stackArray[stackIndex - 1];

                    // Use pre-calculated bounds for more efficient comparisons
                    bool validLeft = cur.X - 2 >= xMin && !map[cur.X - 2, cur.Y];
                    bool validRight = cur.X + 2 < xMax && !map[cur.X + 2, cur.Y];
                    bool validUp = cur.Y - 2 >= yMin && !map[cur.X, cur.Y - 2];
                    bool validDown = cur.Y + 2 < yMax && !map[cur.X, cur.Y + 2];

                    int validLeftByte = Unsafe.As<bool, byte>(ref validLeft);
                    int validRightByte = Unsafe.As<bool, byte>(ref validRight);
                    int validUpByte = Unsafe.As<bool, byte>(ref validUp);
                    int validDownByte = Unsafe.As<bool, byte>(ref validDown);

                    int targetCount = validLeftByte + validRightByte + validUpByte + validDownByte;

                    if (targetCount == 0)
                    {
                        // Pop (remove last element)
                        stackIndex--;
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

                        // Push new point
                        stackArray[stackIndex++] = new MazePoint(nextX, nextY);
                        map[nextXInBetween, nextYInBetween] = true;
                        map[nextX, nextY] = true;

                        pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                        pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                    }
                }

                return new Maze(map);
            }
            finally
            {
                ArrayPool<MazePoint>.Shared.Return(stackArray);
            }
        }
    }
}
