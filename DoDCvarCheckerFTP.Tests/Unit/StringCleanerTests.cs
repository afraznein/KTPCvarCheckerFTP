using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DoDCvarCheckerFTP.Utils;

namespace DoDCvarCheckerFTP.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for StringCleaner class
    /// Tests all public methods including edge cases and performance-critical paths
    /// </summary>
    public class StringCleanerTests
    {
        #region CleanLogLine Tests

        [Fact]
        public void CleanLogLine_RemovesLogPrefixes()
        {
            // Arrange
            // Note: StringCleaner removes " [ktp_cvar.amxx] " (with surrounding spaces)
            // So we need a space before the bracket for it to be removed
            string input = "L  -  [ktp_cvar.amxx] Test message";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.DoesNotContain("L  - ", result);
            Assert.DoesNotContain("[ktp_cvar.amxx]", result);
            Assert.Equal("Test message", result);
        }

        [Fact]
        public void CleanLogLine_RemovesTimestamps()
        {
            // Arrange
            string input = "<0.000000> Test message";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.DoesNotContain("<0.000000>", result);
        }

        [Fact]
        public void CleanLogLine_RemovesMapNames()
        {
            // Arrange
            string input = "Server changed to dod_anzio map";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.DoesNotContain("dod_anzio", result);
        }

        [Fact]
        public void CleanLogLine_RemovesMultipleMapNames()
        {
            // Arrange
            string input = "dod_anzio dod_flash dod_orange";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.DoesNotContain("dod_anzio", result);
            Assert.DoesNotContain("dod_flash", result);
            Assert.DoesNotContain("dod_orange", result);
        }

        [Fact]
        public void CleanLogLine_HandlesNullInput()
        {
            // Arrange
            string input = null;

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CleanLogLine_HandlesEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void CleanLogLine_HandlesSinglePass()
        {
            // Arrange
            // Note: Proper format with space before [ktp_cvar.amxx]
            string input = "L  - <0.000000>  [ktp_cvar.amxx] -------- Mapchange to dod_anzio [AMXX] Test";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            // All replacements should happen in one pass
            Assert.DoesNotContain("L  - ", result);
            Assert.DoesNotContain("<0.000000>", result);
            Assert.DoesNotContain("[ktp_cvar.amxx]", result);
            Assert.DoesNotContain("--------", result);
            Assert.DoesNotContain("Mapchange to", result);
            Assert.DoesNotContain("dod_anzio", result);
            Assert.DoesNotContain("[AMXX]", result);
        }

        [Fact]
        public void CleanLogLine_CaseInsensitiveMapNames()
        {
            // Arrange
            string input = "DOD_ANZIO DoD_Flash dOd_OrAnGe";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.DoesNotContain("DOD_ANZIO", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DoD_Flash", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("dOd_OrAnGe", result, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region ProcessLogLinesOptimized Tests

        [Fact]
        public void ProcessLogLinesOptimized_HandlesEmptyList()
        {
            // Arrange
            var input = new List<string>();

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(input);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ProcessLogLinesOptimized_FiltersOutEmptyLines()
        {
            // Arrange
            var input = new List<string> { "Valid line", "", "  ", null, "Another valid" };

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(input);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ProcessLogLinesOptimized_HandlesLargeDataset()
        {
            // Arrange
            // Note: StringCleaner removes " [ktp_cvar.amxx] " (with surrounding spaces)
            // Use proper format that matches production logs
            var input = Enumerable.Range(0, 10000)
                .Select(i => $"L  -  [ktp_cvar.amxx] Line {i}")
                .ToList();

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(input);

            // Assert
            Assert.Equal(10000, result.Count);
            Assert.All(result, line => Assert.DoesNotContain("[ktp_cvar.amxx]", line));
        }

        [Fact]
        public void ProcessLogLinesOptimized_TrimsWhitespace()
        {
            // Arrange
            var input = new List<string> { "  Test line  ", "\tAnother\t", "   " };

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(input);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, line => Assert.Equal(line.Trim(), line));
        }

        #endregion

        #region ExtractSteamID Tests

        [Fact]
        public void ExtractSteamID_ValidFormat_ReturnsID()
        {
            // Arrange
            string line = "STEAMID:0:1:12345678 | Player | 192.168.1.1";

            // Act
            string result = StringCleaner.ExtractSteamID(line);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("STEAMID:", result);
        }

        [Fact]
        public void ExtractSteamID_NoID_ReturnsNull()
        {
            // Arrange
            string line = "Just a regular log line";

            // Act
            string result = StringCleaner.ExtractSteamID(line);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractSteamID_EmptyString_ReturnsNull()
        {
            // Arrange
            string line = "";

            // Act
            string result = StringCleaner.ExtractSteamID(line);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ExtractIPAddress Tests

        [Fact]
        public void ExtractIPAddress_ValidFormat_ReturnsIP()
        {
            // Arrange
            string line = "STEAMID:0:1:12345678 | Player | 192.168.1.100";

            // Act
            string result = StringCleaner.ExtractIPAddress(line);

            // Assert
            Assert.NotNull(result);
            Assert.Matches(@"\d+\.\d+\.\d+\.\d+", result);
        }

        [Fact]
        public void ExtractIPAddress_NoIP_ReturnsNull()
        {
            // Arrange
            string line = "No IP address here";

            // Act
            string result = StringCleaner.ExtractIPAddress(line);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractIPAddress_MultipleIPs_ReturnsFirst()
        {
            // Arrange
            string line = "192.168.1.1 and 10.0.0.1";

            // Act
            string result = StringCleaner.ExtractIPAddress(line);

            // Assert
            Assert.NotNull(result);
            // Should return the first IP
            Assert.StartsWith("192.168", result);
        }

        #endregion

        #region IsKTPViolation Tests

        [Fact]
        public void IsKTPViolation_ValidViolation_ReturnsTrue()
        {
            // Arrange
            string line = "STEAMID:0:1:123 | Player | 1.1.1.1 | Invalid r_fullbright: 1.0 (KTP value: 0.0)";

            // Act
            bool result = StringCleaner.IsKTPViolation(line);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsKTPViolation_ContainsHudTakesshots_ReturnsFalse()
        {
            // Arrange
            string line = "KTP value hud_takesshots violation";

            // Act
            bool result = StringCleaner.IsKTPViolation(line);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsKTPViolation_NoKTPValue_ReturnsFalse()
        {
            // Arrange
            string line = "Just a regular log line";

            // Act
            bool result = StringCleaner.IsKTPViolation(line);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsRateCvar Tests

        [Theory]
        [InlineData("cl_updaterate", true)]
        [InlineData("cl_cmdrate", true)]
        [InlineData("rate", true)]
        [InlineData("cl_interp", true)]
        [InlineData("net_graph", true)]
        [InlineData("r_fullbright", false)]
        [InlineData("gl_polyoffset", false)]
        public void IsRateCvar_VariousCvars_ReturnsCorrectly(string cvarName, bool expected)
        {
            // Arrange
            string line = $"Invalid {cvarName}: value";

            // Act
            bool result = StringCleaner.IsRateCvar(line);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region SplitLogLine Tests

        [Fact]
        public void SplitLogLine_ValidFormat_ParsesCorrectly()
        {
            // Arrange
            // SplitLogLine expects: "STEAMID | PlayerName IP | Violation" format
            // where IP is within the same segment as PlayerName
            string line = "STEAMID:0:1:12345678 | PlayerName 192.168.1.1 | Invalid r_fullbright: 1.0 (KTP value: 0.0)";

            // Act
            var (steamId, playerName, ip, violation) = StringCleaner.SplitLogLine(line);

            // Assert
            Assert.Contains("STEAMID", steamId);
            Assert.NotNull(playerName);
            Assert.NotNull(ip);
            Assert.NotNull(violation);
        }

        [Fact]
        public void SplitLogLine_MissingParts_ReturnsNulls()
        {
            // Arrange
            string line = "Invalid line";

            // Act
            var (steamId, playerName, ip, violation) = StringCleaner.SplitLogLine(line);

            // Assert
            Assert.Null(steamId);
            Assert.Null(playerName);
            Assert.Null(ip);
            Assert.Null(violation);
        }

        #endregion

        #region CleanAndNormalizeLogLines Tests

        [Fact]
        public void CleanAndNormalizeLogLines_RemovesDuplicates()
        {
            // Arrange
            var input = new List<string>
            {
                "Same line",
                "Same line",
                "Different line"
            };

            // Act
            var result = StringCleaner.CleanAndNormalizeLogLines(input);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void CleanAndNormalizeLogLines_RemovesTimestamps()
        {
            // Arrange
            var input = new List<string>
            {
                "L 11/17/2025 - 14:23:45: Test message"
            };

            // Act
            var result = StringCleaner.CleanAndNormalizeLogLines(input);

            // Assert
            Assert.All(result, line => Assert.DoesNotContain("11/17/2025", line));
            Assert.All(result, line => Assert.DoesNotContain("14:23:45", line));
        }

        [Fact]
        public void CleanAndNormalizeLogLines_HandlesEmptyAndWhitespace()
        {
            // Arrange
            var input = new List<string> { "", "  ", "\t", "Valid" };

            // Act
            var result = StringCleaner.CleanAndNormalizeLogLines(input);

            // Assert
            Assert.Single(result);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void CleanLogLine_Performance_UnderMillisecond()
        {
            // Arrange
            string testLine = "L  - <0.000000> [ktp_cvar.amxx] -------- dod_anzio [AMXX] Test";
            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                StringCleaner.CleanLogLine(testLine);
            }
            timer.Stop();

            // Assert
            // 1000 iterations should complete in under 100ms (average < 0.1ms per call)
            Assert.True(timer.ElapsedMilliseconds < 100,
                $"Expected < 100ms, got {timer.ElapsedMilliseconds}ms");
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void ProcessLogLinesOptimized_Performance_LinearScaling(int lineCount)
        {
            // Arrange
            var testLines = Enumerable.Range(0, lineCount)
                .Select(i => $"L  - [ktp_cvar.amxx] Test line {i} dod_anzio")
                .ToList();

            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(testLines);
            timer.Stop();

            // Assert
            Assert.Equal(lineCount, result.Count);

            // Should scale linearly: ~1ms per 1000 lines max
            // Allow up to 20ms per 1000 lines for code coverage overhead
            double msPerThousandLines = (timer.ElapsedMilliseconds * 1000.0) / lineCount;
            Assert.True(msPerThousandLines < 20,
                $"Expected < 20ms per 1000 lines, got {msPerThousandLines:F2}ms");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CleanLogLine_UnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            string input = "L  - Player名前 with unicode";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Player名前", result);
        }

        [Fact]
        public void CleanLogLine_SpecialCharactersInName_Preserves()
        {
            // Arrange
            string input = "Player<>[]{}!@#$%^&*()";

            // Act
            string result = StringCleaner.CleanLogLine(input);

            // Assert
            Assert.Contains("Player<>[]{}!@#$%^&*()", result);
        }

        [Fact]
        public void ProcessLogLinesOptimized_MixedLineEndings_Handles()
        {
            // Arrange
            var input = new List<string>
            {
                "Line 1\r\n",
                "Line 2\n",
                "Line 3\r",
                "Line 4"
            };

            // Act
            var result = StringCleaner.ProcessLogLinesOptimized(input);

            // Assert
            Assert.Equal(4, result.Count);
        }

        #endregion
    }
}
