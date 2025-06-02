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
    /// Optimized version using ArrayPool for stack allocation to reduce heap allocations
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation : IAlgorithm<Maze>
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

            // Use ArrayPool for better memory management
            // Conservative stack size - in worst case backtracking can use substantial stack
            int maxStackSize = Math.Max(8192, width * height / 8);
            var stackArray = ArrayPool<MazePoint>.Shared.Rent(maxStackSize);
            int stackTop = -1;

            try
            {
                // Push initial point
                stackArray[++stackTop] = new MazePoint(1, 1);
                map[1, 1] = true;

                pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

                while (stackTop >= 0)
                {
                    MazePoint cur = stackArray[stackTop];

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
                        stackTop--; // Pop from array-based stack
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

                        // Push to array-based stack with bounds checking
                        if (stackTop >= stackArray.Length - 1)
                        {
                            throw new InvalidOperationException($"Stack overflow - maze too complex for array-based stack. Stack size: {stackArray.Length}, requested: {stackTop + 1}");
                        }
                        
                        stackArray[++stackTop] = new MazePoint(nextX, nextY);
                        map[nextXInBetween, nextYInBetween] = true;
                        map[nextX, nextY] = true;

                        pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                        pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                    }
                }
            }
            finally
            {
                ArrayPool<MazePoint>.Shared.Return(stackArray, clearArray: false);
            }

            return new Maze(map);
        }
    }
}