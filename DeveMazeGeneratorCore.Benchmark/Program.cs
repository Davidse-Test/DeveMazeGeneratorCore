using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace DeveMazeGeneratorCore.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Running the Benchmark job");

            // Create the BenchmarkDotNet.Artifacts directory if it doesn't exist
            Directory.CreateDirectory("BenchmarkDotNet.Artifacts");
            Directory.CreateDirectory("BenchmarkDotNet.Artifacts/results");

            //var config = DefaultConfig.Instance.WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(200));
            var summary = BenchmarkRunner.Run<MazeBenchmarkJob>();

            Console.WriteLine("Benchmark job completed");
            Console.WriteLine($"Results saved to: {Path.GetFullPath("BenchmarkDotNet.Artifacts/results")}");
        }
    }
}