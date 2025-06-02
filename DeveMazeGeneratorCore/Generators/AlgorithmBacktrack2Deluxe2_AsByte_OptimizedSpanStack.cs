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
    /// Optimized version using stackalloc Span for true stack allocation with zero heap allocations
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSpanStack : IAlgorithm<Maze>
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

            // Estimate needed stack size - backtracking can go deep
            int estimatedStackSize = Math.Max(512, (width * height) / 32);
            
            // Use span-based stack only for smaller mazes where it's safe
            if (estimatedStackSize <= 512)
            {
                return GoGenerateInternalWithSpan(map, random, pixelChangedCallback, estimatedStackSize);
            }
            else
            {
                // Fall back to regular Stack<T> for larger mazes
                return GoGenerateInternalWithStack(map, random, pixelChangedCallback);
            }
        }

        private Maze GoGenerateInternalWithSpan<M, TAction>(M map, IRandom random, TAction pixelChangedCallback, int stackSize) where M : InnerMap where TAction : struct, IProgressAction
        {
            long totSteps = (map.Width - 1L) / 2L * ((map.Height - 1L) / 2L);
            long currentStep = 1;

            int width = map.Width - 1;
            int height = map.Height - 1;

            // Use stackalloc for true stack allocation - much faster than heap allocation
            Span<MazePoint> stackSpan = stackalloc MazePoint[stackSize];
            int stackTop = -1;

            // Push initial point
            stackSpan[++stackTop] = new MazePoint(1, 1);
            map[1, 1] = true;

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            while (stackTop >= 0)
            {
                MazePoint cur = stackSpan[stackTop];

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
                    stackTop--; // Pop from span-based stack
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

                    // Check stack bounds and fall back to heap if needed
                    if (stackTop >= stackSpan.Length - 1)
                    {
                        // Stack overflow imminent, fall back to heap-based implementation
                        return GoGenerateInternalWithStackFromSpan(map, random, pixelChangedCallback, stackSpan, stackTop, nextX, nextY, nextXInBetween, nextYInBetween);
                    }
                    
                    stackSpan[++stackTop] = new MazePoint(nextX, nextY);
                    map[nextXInBetween, nextYInBetween] = true;
                    map[nextX, nextY] = true;

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        private Maze GoGenerateInternalWithStack<M, TAction>(M map, IRandom random, TAction pixelChangedCallback) where M : InnerMap where TAction : struct, IProgressAction
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

                    pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
                    pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);
                }
            }

            return new Maze(map);
        }

        private Maze GoGenerateInternalWithStackFromSpan<M, TAction>(M map, IRandom random, TAction pixelChangedCallback, 
            Span<MazePoint> existingStack, int existingStackTop, int nextX, int nextY, int nextXInBetween, int nextYInBetween) 
            where M : InnerMap where TAction : struct, IProgressAction
        {
            long totSteps = (map.Width - 1L) / 2L * ((map.Height - 1L) / 2L);
            long currentStep = 1;

            int width = map.Width - 1;
            int height = map.Height - 1;

            // Transfer existing span contents to heap-based stack
            var stackje = new Stack<MazePoint>();
            for (int i = 0; i <= existingStackTop; i++)
            {
                stackje.Push(existingStack[i]);
            }

            // Continue with the pending move
            stackje.Push(new MazePoint(nextX, nextY));
            map[nextXInBetween, nextYInBetween] = true;
            map[nextX, nextY] = true;

            pixelChangedCallback.Invoke(nextXInBetween, nextYInBetween, currentStep, totSteps);
            pixelChangedCallback.Invoke(nextX, nextY, currentStep, totSteps);

            // Continue with regular stack-based algorithm
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

                    nextX = cur.X + actuallyGoingLeftByte * -2 + actuallyGoingRightByte * 2;
                    nextY = cur.Y + actuallyGoingUpByte * -2 + actuallyGoingDownByte * 2;

                    nextXInBetween = cur.X - actuallyGoingLeftByte + actuallyGoingRightByte;
                    nextYInBetween = cur.Y - actuallyGoingUpByte + actuallyGoingDownByte;

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