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
    /// Optimized version using lookup tables and bit manipulation for direction selection
    /// </summary>
    public class AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection : IAlgorithm<Maze>
    {
        // Pre-calculated lookup tables for direction offsets
        private static readonly int[] DxOffsets = { -2, 2, 0, 0 };  // Left, Right, Up, Down
        private static readonly int[] DyOffsets = { 0, 0, -2, 2 };  // Left, Right, Up, Down
        private static readonly int[] DxInBetween = { -1, 1, 0, 0 }; // Left, Right, Up, Down
        private static readonly int[] DyInBetween = { 0, 0, -1, 1 }; // Left, Right, Up, Down

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

            pixelChangedCallback.Invoke(1, 1, currentStep, totSteps);

            while (stackje.Count != 0)
            {
                MazePoint cur = stackje.Peek();

                // Use lookup table approach with bit packing
                byte validDirections = 0;
                
                // Check all directions using lookup tables
                if (cur.X - 2 > 0 && !map[cur.X - 2, cur.Y]) validDirections |= 1; // Left
                if (cur.X + 2 < width && !map[cur.X + 2, cur.Y]) validDirections |= 2; // Right
                if (cur.Y - 2 > 0 && !map[cur.X, cur.Y - 2]) validDirections |= 4; // Up
                if (cur.Y + 2 < height && !map[cur.X, cur.Y + 2]) validDirections |= 8; // Down

                // Count valid directions using bit counting
                int targetCount = CountBits(validDirections);

                if (targetCount == 0)
                {
                    stackje.Pop();
                }
                else
                {
                    var chosenDirection = random.Next(targetCount);
                    
                    // Find the chosen direction using lookup
                    int directionIndex = GetDirectionIndex(validDirections, chosenDirection);
                    
                    var nextX = cur.X + DxOffsets[directionIndex];
                    var nextY = cur.Y + DyOffsets[directionIndex];
                    var nextXInBetween = cur.X + DxInBetween[directionIndex];
                    var nextYInBetween = cur.Y + DyInBetween[directionIndex];

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
        private static int CountBits(byte value)
        {
            // Fast bit counting
            value = (byte)(value - ((value >> 1) & 0x55));
            value = (byte)((value & 0x33) + ((value >> 2) & 0x33));
            return (value + (value >> 4)) & 0x0F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDirectionIndex(byte validDirections, int chosenDirection)
        {
            // Find the nth set bit (chosenDirection) in validDirections
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if ((validDirections & (1 << i)) != 0)
                {
                    if (count == chosenDirection)
                        return i;
                    count++;
                }
            }
            return 0; // Should never reach here
        }
    }
}