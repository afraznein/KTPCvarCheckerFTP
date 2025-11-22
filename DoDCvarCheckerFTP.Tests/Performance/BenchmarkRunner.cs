using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace DoDCvarCheckerFTP.Tests.Performance
{
    /// <summary>
    /// Entry point for running performance benchmarks
    /// Run this to generate detailed performance comparison reports
    ///
    /// Usage: Create a separate console project or call manually:
    ///   BenchmarkRunnerHelper.RunAllBenchmarks();
    /// </summary>
    public class BenchmarkRunnerHelper
    {
        /// <summary>
        /// Run all performance benchmarks
        /// </summary>
        public static void RunAllBenchmarks()
        {
            // Configure benchmarks
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

            // Run string cleaning benchmark
            var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<StringCleaningBenchmark>(config);

            // Results will be saved to BenchmarkDotNet.Artifacts/results/
        }
    }
}
