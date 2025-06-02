using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using DeveMazeGeneratorCore.Factories;
using DeveMazeGeneratorCore.Generators;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Mazes;
using System.Collections.Generic;

namespace DeveMazeGeneratorCore.Benchmark
{
    [MemoryDiagnoser]
    [
        DeveJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 1, targetCount: 3, invocationCount: 1),
    ]
    [Config(typeof(Config))]
    public class QuickOptimizedBenchmark
    {
        private const int SMALL_SIZE = 1024; // Much smaller size for quick testing
        private const int SEED = 1337;

        private InnerMapFactory<BitArreintjeFastInnerMap> _innerMapFactory = new InnerMapFactory<BitArreintjeFastInnerMap>();
        private RandomFactory<XorShiftRandom> _randomFactory = new RandomFactory<XorShiftRandom>();
        private NoAction _action = new NoAction();

        public IEnumerable<object> OptimizedAlgorithms()
        {
            // Original for comparison
            yield return new AlgorithmBacktrack2Deluxe2_AsByte();
            // Individual optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMazePointStructure();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedRandomNumber();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks();
            // Additional individual optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSpanStack();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBranchPrediction();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedInlining();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedLoopUnrolling();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBitmaskDirections();
            // Combined optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombined();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombinedImproved();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBestCombined();
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(OptimizedAlgorithms))]
        public void QuickBenchmark(IAlgorithm<Maze> algorithm)
        {
            algorithm.GoGenerate(SMALL_SIZE, SMALL_SIZE, SEED, _innerMapFactory, _randomFactory, _action);
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                SummaryStyle = SummaryStyle.Default.WithMaxParameterColumnWidth(200);
            }
        }
    }
}