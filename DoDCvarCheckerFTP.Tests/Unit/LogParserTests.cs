using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DoDCvarCheckerFTP.Core.Logging;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for LogParser class
    /// Tests all parsing methods, edge cases, and filtering logic
    /// </summary>
    public class LogParserTests
    {
        #region ParseCvarViolation Tests

        [Fact]
        public void ParseCvarViolation_ValidLine_ReturnsViolation()
        {
            // Arrange
            string logLine = "L 11/17/2025 - 14:23:45: [ktp_cvar.amxx] STEAMID:0:1:12345678 | TestPlayer | 192.168.1.100 | Invalid r_fullbright: 1.0 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine, "TestServer");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("STEAMID:0:1:12345678", result.SteamID);
            Assert.Equal("TestPlayer", result.PlayerName);
            Assert.Equal("192.168.1.100", result.IPAddress);
            Assert.Equal("TestServer", result.ServerHostname);
        }

        [Fact]
        public void ParseCvarViolation_ValidLineWithoutTimestamp_ReturnsViolation()
        {
            // Arrange
            string logLine = "STEAMID:0:1:999 | Player2 | 10.0.0.1 | Invalid gl_polyoffset: 1.0 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("STEAMID:0:1:999", result.SteamID);
        }

        [Fact]
        public void ParseCvarViolation_InvalidLine_ReturnsNull()
        {
            // Arrange
            string logLine = "This is not a violation line";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseCvarViolation_NullInput_ReturnsNull()
        {
            // Arrange
            string logLine = null;

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseCvarViolation_EmptyString_ReturnsNull()
        {
            // Arrange
            string logLine = "";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsSteamID()
        {
            // Arrange
            string logLine = "STEAMID:0:1:12345678 | Player | 1.1.1.1 | Invalid r_fullbright: 1.0 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("STEAMID:", result.SteamID);
            Assert.Contains("12345678", result.SteamID);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsPlayerName()
        {
            // Arrange
            string logLine = "STEAMID:0:1:111 | MyTestPlayer | 2.2.2.2 | Invalid rate: 10000 (KTP value: 20000)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MyTestPlayer", result.PlayerName);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsIPAddress()
        {
            // Arrange
            string logLine = "STEAMID:0:1:222 | Player | 192.168.50.100 | Invalid cl_interp: 0.1 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("192.168.50.100", result.IPAddress);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsCvarName()
        {
            // Arrange
            string logLine = "STEAMID:0:1:333 | Player | 1.1.1.1 | Invalid gl_polyoffset: 0.1 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("gl_polyoffset", result.CvarName, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsInvalidValue()
        {
            // Arrange
            string logLine = "STEAMID:0:1:444 | Player | 1.1.1.1 | Invalid r_fullbright: 999 (KTP value: 0.0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("999", result.InvalidValue);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsRequiredValue()
        {
            // Arrange
            string logLine = "STEAMID:0:1:555 | Player | 1.1.1.1 | Invalid test_cvar: 1.0 (KTP value: 2.5)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("2.5", result.RequiredValue);
        }

        [Fact]
        public void ParseCvarViolation_ExtractsTimestamp()
        {
            // Arrange
            string logLine = "L 11/17/2025 - 14:23:45: STEAMID:0:1:666 | Player | 1.1.1.1 | Invalid test: 1 (KTP value: 0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(default(DateTime), result.Timestamp);
        }

        [Fact]
        public void ParseCvarViolation_SetsRawLogLine()
        {
            // Arrange
            string logLine = "STEAMID:0:1:777 | Player | 1.1.1.1 | Invalid test: 1 (KTP value: 0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(logLine, result.RawLogLine);
        }

        #endregion

        #region ParseCvarViolations (Multiple Lines) Tests

        [Fact]
        public void ParseCvarViolations_MultipleLinesCorrectly()
        {
            // Arrange
            var logLines = new List<string>
            {
                "STEAMID:0:1:111 | Player1 | 1.1.1.1 | Invalid r_fullbright: 1 (KTP value: 0)",
                "STEAMID:0:1:222 | Player2 | 2.2.2.2 | Invalid gl_polyoffset: 1 (KTP value: 0)",
                "Not a violation line",
                "STEAMID:0:1:333 | Player3 | 3.3.3.3 | Invalid rate: 10000 (KTP value: 20000)"
            };

            // Act
            var results = LogParser.ParseCvarViolations(logLines, "TestServer");

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, v => Assert.Equal("TestServer", v.ServerHostname));
        }

        [Fact]
        public void ParseCvarViolations_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var logLines = new List<string>();

            // Act
            var results = LogParser.ParseCvarViolations(logLines);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public void ParseCvarViolations_NoValidViolations_ReturnsEmpty()
        {
            // Arrange
            var logLines = new List<string>
            {
                "Just a log line",
                "Another log line",
                "No violations here"
            };

            // Act
            var results = LogParser.ParseCvarViolations(logLines);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public void ParseCvarViolations_MixedValidAndInvalid_ParsesValidOnly()
        {
            // Arrange
            var logLines = new List<string>
            {
                "STEAMID:0:1:111 | Player1 | 1.1.1.1 | Invalid test: 1 (KTP value: 0)",
                "",
                null,
                "Invalid line",
                "STEAMID:0:1:222 | Player2 | 2.2.2.2 | Invalid test2: 1 (KTP value: 0)"
            };

            // Act
            var results = LogParser.ParseCvarViolations(logLines);

            // Assert
            Assert.Equal(2, results.Count);
        }

        #endregion

        #region FilterViolations Tests

        [Fact]
        public void FilterViolations_IgnoreRates_FiltersCorrectly()
        {
            // Arrange
            var violations = new List<CvarViolation>
            {
                new CvarViolation { CvarName = "r_fullbright", SteamID = "STEAMID:0:1:111" },
                new CvarViolation { CvarName = "rate", SteamID = "STEAMID:0:1:222" },
                new CvarViolation { CvarName = "cl_updaterate", SteamID = "STEAMID:0:1:333" },
                new CvarViolation { CvarName = "gl_polyoffset", SteamID = "STEAMID:0:1:444" }
            };

            // Act
            var results = LogParser.FilterViolations(violations, ignoreRates: true);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.DoesNotContain(results, v => v.IsRateViolation);
        }

        [Fact]
        public void FilterViolations_DateRange_FiltersCorrectly()
        {
            // Arrange
            var violations = new List<CvarViolation>
            {
                new CvarViolation { CvarName = "test1", Timestamp = new DateTime(2025, 11, 1) },
                new CvarViolation { CvarName = "test2", Timestamp = new DateTime(2025, 11, 15) },
                new CvarViolation { CvarName = "test3", Timestamp = new DateTime(2025, 11, 30) }
            };

            // Act
            var results = LogParser.FilterViolations(
                violations,
                startDate: new DateTime(2025, 11, 10),
                endDate: new DateTime(2025, 11, 20)
            );

            // Assert
            Assert.Single(results);
            Assert.Equal("test2", results[0].CvarName);
        }

        [Fact]
        public void FilterViolations_SteamID_FiltersCorrectly()
        {
            // Arrange
            var violations = new List<CvarViolation>
            {
                new CvarViolation { SteamID = "STEAMID:0:1:111", CvarName = "test1" },
                new CvarViolation { SteamID = "STEAMID:0:1:222", CvarName = "test2" },
                new CvarViolation { SteamID = "STEAMID:0:1:111", CvarName = "test3" }
            };

            // Act
            var results = LogParser.FilterViolations(violations, steamIdFilter: "STEAMID:0:1:111");

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, v => Assert.Equal("STEAMID:0:1:111", v.SteamID));
        }

        [Fact]
        public void FilterViolations_CombinedFilters_AppliesAll()
        {
            // Arrange
            var violations = new List<CvarViolation>
            {
                new CvarViolation
                {
                    SteamID = "STEAMID:0:1:111",
                    CvarName = "r_fullbright",
                    Timestamp = new DateTime(2025, 11, 15)
                },
                new CvarViolation
                {
                    SteamID = "STEAMID:0:1:111",
                    CvarName = "rate",
                    Timestamp = new DateTime(2025, 11, 15)
                },
                new CvarViolation
                {
                    SteamID = "STEAMID:0:1:222",
                    CvarName = "r_fullbright",
                    Timestamp = new DateTime(2025, 11, 15)
                }
            };

            // Act
            var results = LogParser.FilterViolations(
                violations,
                ignoreRates: true,
                steamIdFilter: "STEAMID:0:1:111",
                startDate: new DateTime(2025, 11, 1)
            );

            // Assert
            Assert.Single(results);
            Assert.Equal("r_fullbright", results[0].CvarName);
            Assert.Equal("STEAMID:0:1:111", results[0].SteamID);
        }

        #endregion

        #region ParseMultiServerLogs Tests

        [Fact]
        public void ParseMultiServerLogs_ParsesAllServers()
        {
            // Arrange
            var serverLogs = new Dictionary<string, List<string>>
            {
                ["Server1"] = new List<string>
                {
                    "STEAMID:0:1:111 | Player1 | 1.1.1.1 | Invalid test: 1 (KTP value: 0)"
                },
                ["Server2"] = new List<string>
                {
                    "STEAMID:0:1:222 | Player2 | 2.2.2.2 | Invalid test: 1 (KTP value: 0)"
                }
            };

            // Act
            var results = LogParser.ParseMultiServerLogs(serverLogs);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.True(results.ContainsKey("Server1"));
            Assert.True(results.ContainsKey("Server2"));
            Assert.Single(results["Server1"]);
            Assert.Single(results["Server2"]);
        }

        [Fact]
        public void ParseMultiServerLogs_EmptyServerLogs_ReturnsEmpty()
        {
            // Arrange
            var serverLogs = new Dictionary<string, List<string>>();

            // Act
            var results = LogParser.ParseMultiServerLogs(serverLogs);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ParseCvarViolation_MalformedLine_HandlesGracefully()
        {
            // Arrange
            string logLine = "STEAMID | | | Invalid (KTP value: )";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            // Should not throw, may return null or partial data
            // As long as it doesn't crash, test passes
            Assert.True(true);
        }

        [Fact]
        public void ParseCvarViolation_UnicodeCharacters_Handles()
        {
            // Arrange
            string logLine = "STEAMID:0:1:999 | Player名前 | 1.1.1.1 | Invalid test: 1 (KTP value: 0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Player名前", result.PlayerName);
        }

        [Fact]
        public void ParseCvarViolation_SpecialCharactersInName_Handles()
        {
            // Arrange
            string logLine = "STEAMID:0:1:999 | Player<>[]{}!@# | 1.1.1.1 | Invalid test: 1 (KTP value: 0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ParseCvarViolation_VeryLongLine_Handles()
        {
            // Arrange
            string longName = new string('A', 1000);
            string logLine = $"STEAMID:0:1:999 | {longName} | 1.1.1.1 | Invalid test: 1 (KTP value: 0)";

            // Act
            var result = LogParser.ParseCvarViolation(logLine);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ParseCvarViolations_LargeDataset_PerformanceAcceptable()
        {
            // Arrange
            var logLines = Enumerable.Range(0, 10000)
                .Select(i => $"STEAMID:0:1:{i} | Player{i} | 1.1.1.{i % 255} | Invalid test: 1 (KTP value: 0)")
                .ToList();

            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var results = LogParser.ParseCvarViolations(logLines);
            timer.Stop();

            // Assert
            Assert.Equal(10000, results.Count);
            // Should parse 10k lines in under 1 second
            Assert.True(timer.ElapsedMilliseconds < 1000,
                $"Expected < 1000ms, got {timer.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}
