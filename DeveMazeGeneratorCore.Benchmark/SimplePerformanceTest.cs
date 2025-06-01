using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using System;
using System.Diagnostics;

namespace DeveMazeGeneratorCore.Benchmark
{
    /// <summary>
    /// Simple performance test to quickly compare the optimized algorithms
    /// </summary>
    public class SimplePerformanceTest
    {
        private const int TEST_SIZE = 512;  // Small size for quick testing
        private const int TEST_ITERATIONS = 5;
        private const int SEED = 1337;

        public static void Main()
        {
            Console.WriteLine("Simple Performance Test for Optimized Algorithms");
            Console.WriteLine($"Maze Size: {TEST_SIZE}x{TEST_SIZE}");
            Console.WriteLine($"Iterations: {TEST_ITERATIONS}");
            Console.WriteLine();

            var innerMapFactory = new InnerMapFactory<BitArreintjeFastInnerMap>();
            var randomFactory = new RandomFactory<XorShiftRandom>();
            var noAction = new NoAction();

            var algorithms = new (string name, IAlgorithm<Mazes.Maze> algorithm)[]
            {
                ("Original", new AlgorithmBacktrack2Deluxe2_AsByte()),
                ("StackAllocation", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation()),
                ("MazePointStructure", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMazePointStructure()),
                ("DirectionSelection", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection()),
                ("RandomNumber", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedRandomNumber()),
                ("Callbacks", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks()),
                ("Combined", new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombined())
            };

            Console.WriteLine($"{"Algorithm",-20} {"Mean (ms)",-12} {"Min (ms)",-12} {"Max (ms)",-12} {"Improvement",-12}");
            Console.WriteLine(new string('-', 75));

            double baselineTime = 0;

            foreach (var (name, algorithm) in algorithms)
            {
                var times = new double[TEST_ITERATIONS];
                
                // Warm up
                algorithm.GoGenerate(TEST_SIZE, TEST_SIZE, SEED, innerMapFactory, randomFactory, noAction);

                // Actual test
                for (int i = 0; i < TEST_ITERATIONS; i++)
                {
                    var sw = Stopwatch.StartNew();
                    algorithm.GoGenerate(TEST_SIZE, TEST_SIZE, SEED + i, innerMapFactory, randomFactory, noAction);
                    sw.Stop();
                    times[i] = sw.Elapsed.TotalMilliseconds;
                }

                var mean = times.Average();
                var min = times.Min();
                var max = times.Max();

                if (name == "Original")
                {
                    baselineTime = mean;
                    Console.WriteLine($"{name,-20} {mean:F2}      {min:F2}      {max:F2}      {"baseline",-12}");
                }
                else
                {
                    var improvement = ((baselineTime - mean) / baselineTime) * 100;
                    var sign = improvement >= 0 ? "+" : "";
                    Console.WriteLine($"{name,-20} {mean:F2}      {min:F2}      {max:F2}      {sign}{improvement:F1}%");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Note: Positive improvement percentage means the optimization is faster than the original.");
        }
    }

    // Extension method for calculating average
    public static class Extensions
    {
        public static double Average(this double[] values)
        {
            double sum = 0;
            foreach (var value in values)
                sum += value;
            return sum / values.Length;
        }

        public static double Min(this double[] values)
        {
            double min = values[0];
            foreach (var value in values)
                if (value < min) min = value;
            return min;
        }

        public static double Max(this double[] values)
        {
            double max = values[0];
            foreach (var value in values)
                if (value > max) max = value;
            return max;
        }
    }
}