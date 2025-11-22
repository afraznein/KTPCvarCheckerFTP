using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DoDCvarCheckerFTP.Utils;
using DoDCvarCheckerFTP.Tests.Helpers;

namespace DoDCvarCheckerFTP.Tests.Performance
{
    /// <summary>
    /// Benchmark comparing old multi-pass string cleaning vs new single-pass
    /// Expected Result: 50-80x faster with new method
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class StringCleaningBenchmark
    {
        private List<string> _testData1K;
        private List<string> _testData10K;
        private List<string> _testData100K;

        [GlobalSetup]
        public void Setup()
        {
            // Generate realistic test data
            _testData1K = TestDataGenerator.GenerateLogLines(1000);
            _testData10K = TestDataGenerator.GenerateLogLines(10000);
            _testData100K = TestDataGenerator.GenerateLogLines(100000);
        }

        #region Old Method Simulations (v1.0.0)

        [Benchmark(Baseline = true)]
        [Arguments(1000)]
        public List<string> Old_MultiplePasses_1K(int count)
        {
            return Old_MultiplePasses(_testData1K);
        }

        [Benchmark]
        [Arguments(10000)]
        public List<string> Old_MultiplePasses_10K(int count)
        {
            return Old_MultiplePasses(_testData10K);
        }

        [Benchmark]
        [Arguments(100000)]
        public List<string> Old_MultiplePasses_100K(int count)
        {
            return Old_MultiplePasses(_testData100K);
        }

        /// <summary>
        /// Simulates old method with 80+ LINQ operations
        /// Each .Select().ToList() creates a new list, causing O(n) overhead per operation
        /// </summary>
        private List<string> Old_MultiplePasses(List<string> lines)
        {
            var result = lines
                .Select(s => s.Replace("L  - ", ""))
                .Select(s => s.Replace("<0.000000>", ""))
                .Select(s => s.Replace(" [ktp_cvar.amxx] ", ""))
                .Select(s => s.Replace("--------", ""))
                .Select(s => s.Replace(" Mapchange to ", ""))
                .Select(s => s.Replace("[AMXX] ", ""))
                .Select(s => s.Replace("dod_anzio", ""))
                .Select(s => s.Replace("dod_flash", ""))
                .Select(s => s.Replace("dod_orange", ""))
                .Select(s => s.Replace("dod_donner", ""))
                .Select(s => s.Replace("dod_saints", ""))
                .Select(s => s.Replace("dod_lennon2", ""))
                .Select(s => s.Replace("dod_thunder2", ""))
                .Select(s => s.Replace("dod_armory", ""))
                .Select(s => s.Replace("dod_railroad", ""))
                .Select(s => s.Replace("dod_aleutian", ""))
                // ... 60 more map names would go here in real code
                .ToList();

            return result;
        }

        #endregion

        #region New Method (v2.0.0)

        [Benchmark]
        [Arguments(1000)]
        public List<string> New_SinglePass_1K(int count)
        {
            return New_SinglePass(_testData1K);
        }

        [Benchmark]
        [Arguments(10000)]
        public List<string> New_SinglePass_10K(int count)
        {
            return New_SinglePass(_testData10K);
        }

        [Benchmark]
        [Arguments(100000)]
        public List<string> New_SinglePass_100K(int count)
        {
            return New_SinglePass(_testData100K);
        }

        /// <summary>
        /// New method using single-pass processing with compiled regex
        /// </summary>
        private List<string> New_SinglePass(List<string> lines)
        {
            return StringCleaner.ProcessLogLinesOptimized(lines);
        }

        #endregion

        #region Single Line Benchmarks

        [Benchmark]
        public string Old_SingleLine_MultiplePasses()
        {
            string line = "L  - <0.000000> [ktp_cvar.amxx] -------- dod_anzio [AMXX] Test message";

            return line
                .Replace("L  - ", "")
                .Replace("<0.000000>", "")
                .Replace(" [ktp_cvar.amxx] ", "")
                .Replace("--------", "")
                .Replace("[AMXX] ", "")
                .Replace("dod_anzio", "");
        }

        [Benchmark]
        public string New_SingleLine_OnePass()
        {
            string line = "L  - <0.000000> [ktp_cvar.amxx] -------- dod_anzio [AMXX] Test message";
            return StringCleaner.CleanLogLine(line);
        }

        #endregion
    }
}
