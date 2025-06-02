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
    //[InliningDiagnoser]
    //[TailCallDiagnoser]
    //[EtwProfiler]
    //[ConcurrencyVisualizerProfiler]
    //[NativeMemoryProfiler]
    //[ThreadingDiagnoser]
    [JsonExporterAttribute.Full]
    [JsonExporterAttribute.FullCompressed]
    [
        //DeveJob(RuntimeMoniker.Net60, launchCount: 1, warmupCount: 4, targetCount: 50, invocationCount: 1),
        //DeveJob(RuntimeMoniker.Net70, launchCount: 1, warmupCount: 4, targetCount: 50, invocationCount: 1),
        DeveJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 4, targetCount: 50, invocationCount: 1),
    ]
    [AsciiDocExporter]
    [HtmlExporter]
    [MarkdownExporterAttribute.GitHub]
    [MinColumn, MaxColumn]
    [Config(typeof(Config))]
    public class MazeBenchmarkJob
    {
        private const int SIZE = 4096 * 2 * 2;
        private const int SEED = 1337;

        private InnerMapFactory<BitArreintjeFastInnerMap> _innerMapFactory = new InnerMapFactory<BitArreintjeFastInnerMap>();
        private RandomFactory<XorShiftRandom> _randomFactory = new RandomFactory<XorShiftRandom>();
        private NoAction _action = new NoAction();

        public IEnumerable<object> Algorithms()
        {
            yield return new AlgorithmBacktrack();
            yield return new AlgorithmBacktrack2();
            yield return new AlgorithmBacktrack2Deluxe_AsByte();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte();
            yield return new AlgorithmBacktrack2Deluxe2WithBorder_AsByte();
            yield return new AlgorithmBacktrack3();
            yield return new AlgorithmBacktrack4();
            //yield return new AlgorithmKruskal();
        }

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
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSpanStack();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBranchPrediction();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedInlining();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedLoopUnrolling();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBitmaskDirections();
            // SIMD and memory layout optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMD();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDAdvanced();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMemoryLayout();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCacheLocality();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDMemoryCombined();
            // Step-by-step SIMD optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep1();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep2();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep3();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep4();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep5();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep6();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep7();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep8();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep9();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedSIMDStep10();
            // Combined optimizations
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombined();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombinedImproved();
            yield return new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedBestCombined();
        }

        [Benchmark]
        [ArgumentsSource(nameof(Algorithms))]
        public void Simple(IAlgorithm<Maze> algorithm)
        {
            algorithm.GoGenerate(SIZE, SIZE, SEED, _innerMapFactory, _randomFactory, _action);
        }

        [Benchmark]
        [ArgumentsSource(nameof(OptimizedAlgorithms))]
        public void OptimizedComparison(IAlgorithm<Maze> algorithm)
        {
            algorithm.GoGenerate(SIZE, SIZE, SEED, _innerMapFactory, _randomFactory, _action);
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
