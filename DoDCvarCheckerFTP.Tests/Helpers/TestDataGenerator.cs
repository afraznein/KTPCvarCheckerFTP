using System;
using System.Collections.Generic;
using System.Linq;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Tests.Helpers
{
    /// <summary>
    /// Generates realistic test data for unit tests and benchmarks
    /// Creates log lines, violations, and reports that match actual production data
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random _random = new Random(42); // Fixed seed for reproducibility

        private static readonly string[] PlayerNames = new[]
        {
            "TestPlayer", "ProGamer", "NewbDestroyer", "SniperElite", "TankMaster",
            "MedicMain", "RiflemanPro", "BazookaGod", "KnifeOnly", "ChickenRunner",
            "Player名前", "JoueurTest", "SpielerTest", "Player<Elite>", "[KTP]Admin"
        };

        private static readonly string[] CvarNames = new[]
        {
            "r_fullbright", "gl_polyoffset", "fps_max", "fps_override",
            "rate", "cl_updaterate", "cl_cmdrate", "cl_interp",
            "net_graph", "ex_interp", "cl_dlmax", "cl_lw",
            "cl_lc", "hud_fastswitch", "violence_hblood"
        };

        private static readonly string[] MapNames = new[]
        {
            "dod_anzio", "dod_flash", "dod_orange", "dod_donner",
            "dod_saints", "dod_lennon2", "dod_thunder2", "dod_armory"
        };

        /// <summary>
        /// Generate a list of realistic log lines
        /// </summary>
        public static List<string> GenerateLogLines(int count)
        {
            var lines = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                // Mix of violation lines and regular log lines
                if (_random.Next(100) < 30) // 30% are violations
                {
                    lines.Add(GenerateValidCvarLogLine());
                }
                else if (_random.Next(100) < 20) // 20% are map changes
                {
                    lines.Add(GenerateMapChangeLine());
                }
                else // 50% are regular server logs
                {
                    lines.Add(GenerateRegularLogLine());
                }
            }

            return lines;
        }

        /// <summary>
        /// Generate a valid CVAR violation log line
        /// Format: "L MM/DD/YYYY - HH:MM:SS: [ktp_cvar.amxx] STEAMID:X:Y:Z | PlayerName | IP | Invalid cvar: value (KTP value: required)"
        /// </summary>
        public static string GenerateValidCvarLogLine()
        {
            var timestamp = GenerateTimestamp();
            var steamId = GenerateSteamID();
            var playerName = PlayerNames[_random.Next(PlayerNames.Length)];
            var ip = GenerateIPAddress();
            var cvarName = CvarNames[_random.Next(CvarNames.Length)];
            var invalidValue = GenerateCvarValue(cvarName);
            var requiredValue = GetRequiredValue(cvarName);

            return $"L {timestamp}: [ktp_cvar.amxx] STEAMID:{steamId} | {playerName} | {ip} | Invalid {cvarName}: {invalidValue} (KTP value: {requiredValue})";
        }

        /// <summary>
        /// Generate an invalid/malformed log line for testing error handling
        /// </summary>
        public static string GenerateInvalidCvarLogLine()
        {
            var templates = new[]
            {
                "Invalid line with no structure",
                "STEAMID | | | Invalid (KTP value: )",
                "STEAMID:0:1: | Player | | Invalid test:",
                "| | | |",
                "",
                "   ",
                new string('X', 10000) // Very long line
            };

            return templates[_random.Next(templates.Length)];
        }

        /// <summary>
        /// Generate a map change log line
        /// </summary>
        public static string GenerateMapChangeLine()
        {
            var timestamp = GenerateTimestamp();
            var mapName = MapNames[_random.Next(MapNames.Length)];

            return $"L {timestamp}: Mapchange to {mapName}";
        }

        /// <summary>
        /// Generate a regular server log line
        /// </summary>
        public static string GenerateRegularLogLine()
        {
            var timestamp = GenerateTimestamp();
            var templates = new[]
            {
                $"L {timestamp}: [AMXX] Plugin loaded successfully",
                $"L {timestamp}: [DODX] Could not load stats file",
                $"L {timestamp}: Server log file started",
                $"L {timestamp}: Player connected",
                $"L {timestamp}: -------- Map loaded --------"
            };

            return templates[_random.Next(templates.Length)];
        }

        /// <summary>
        /// Generate a list of CvarViolation objects
        /// </summary>
        public static List<CvarViolation> GenerateViolations(int count, string serverHostname = null)
        {
            var violations = new List<CvarViolation>(count);

            for (int i = 0; i < count; i++)
            {
                var steamId = GenerateSteamID();
                var cvarName = CvarNames[_random.Next(CvarNames.Length)];

                violations.Add(new CvarViolation
                {
                    SteamID = $"STEAMID:{steamId}",
                    PlayerName = PlayerNames[_random.Next(PlayerNames.Length)],
                    IPAddress = GenerateIPAddress(),
                    CvarName = cvarName,
                    InvalidValue = GenerateCvarValue(cvarName),
                    RequiredValue = GetRequiredValue(cvarName),
                    Timestamp = DateTime.Now.AddHours(-_random.Next(0, 72)),
                    ServerHostname = serverHostname ?? $"TestServer{_random.Next(1, 5)}",
                    RawLogLine = GenerateValidCvarLogLine()
                });
            }

            return violations;
        }

        /// <summary>
        /// Generate a ViolationReport with realistic data
        /// </summary>
        public static ViolationReport GenerateReport(int playerCount, int violationCount)
        {
            var violations = GenerateViolations(violationCount);

            var report = new ViolationReport
            {
                GeneratedAt = DateTime.Now,
                TotalViolationEntries = violationCount,
                ServerCount = 5
            };

            // Aggregate by player into Summaries
            var grouped = violations
                .GroupBy(v => v.SteamID)
                .Take(playerCount)
                .ToList();

            foreach (var group in grouped)
            {
                var summary = new ViolationSummary
                {
                    SteamID = group.Key
                };

                // Add aliases and IP addresses
                foreach (var violation in group)
                {
                    summary.Aliases.Add(violation.PlayerName);
                    summary.IPAddresses.Add(violation.IPAddress);
                }

                // Aggregate violations by cvar
                foreach (var violation in group)
                {
                    if (!summary.CvarViolations.ContainsKey(violation.CvarName))
                    {
                        summary.CvarViolations[violation.CvarName] = 0;
                    }
                    summary.CvarViolations[violation.CvarName]++;
                }

                report.Summaries.Add(summary);
            }

            return report;
        }

        /// <summary>
        /// Generate a timestamp in the format "MM/DD/YYYY - HH:MM:SS"
        /// </summary>
        private static string GenerateTimestamp()
        {
            var date = DateTime.Now.AddDays(-_random.Next(0, 30));
            return $"{date:MM/dd/yyyy} - {date:HH:mm:ss}";
        }

        /// <summary>
        /// Generate a SteamID in the format "X:Y:Z"
        /// </summary>
        private static string GenerateSteamID()
        {
            int universe = _random.Next(0, 2); // 0 or 1
            int type = _random.Next(0, 2);     // 0 or 1
            int accountId = _random.Next(1, 999999999);

            return $"{universe}:{type}:{accountId}";
        }

        /// <summary>
        /// Generate a random IP address
        /// </summary>
        private static string GenerateIPAddress()
        {
            return $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }

        /// <summary>
        /// Generate a realistic cvar value based on cvar name
        /// </summary>
        private static string GenerateCvarValue(string cvarName)
        {
            cvarName = cvarName.ToLower();

            if (cvarName.Contains("rate"))
                return _random.Next(5000, 30000).ToString();

            if (cvarName.Contains("interp"))
                return (_random.NextDouble() * 0.1).ToString("F6");

            if (cvarName.Contains("fps"))
                return _random.Next(60, 300).ToString();

            if (cvarName.Contains("fullbright") || cvarName.Contains("polyoffset"))
                return _random.Next(0, 2).ToString();

            // Default random value
            return _random.NextDouble().ToString("F2");
        }

        /// <summary>
        /// Get the required/expected value for a cvar
        /// </summary>
        private static string GetRequiredValue(string cvarName)
        {
            cvarName = cvarName.ToLower();

            if (cvarName.Contains("rate"))
                return "20000";

            if (cvarName.Contains("cmdrate") || cvarName.Contains("updaterate"))
                return "101";

            if (cvarName.Contains("interp"))
                return "0.000000";

            if (cvarName.Contains("fullbright") || cvarName.Contains("polyoffset"))
                return "0";

            if (cvarName.Contains("fps_max"))
                return "101";

            // Default
            return "0.0";
        }

        /// <summary>
        /// Generate log lines with specific characteristics for testing
        /// </summary>
        public static List<string> GenerateLogLinesWithCharacteristics(
            int totalCount,
            int violationCount = 0,
            int emptyLines = 0,
            int unicodeNames = 0,
            int specialChars = 0)
        {
            var lines = new List<string>(totalCount);

            // Add violations
            for (int i = 0; i < violationCount; i++)
            {
                lines.Add(GenerateValidCvarLogLine());
            }

            // Add empty lines
            for (int i = 0; i < emptyLines; i++)
            {
                lines.Add(_random.Next(2) == 0 ? "" : "   ");
            }

            // Add unicode names
            for (int i = 0; i < unicodeNames; i++)
            {
                var steamId = GenerateSteamID();
                var ip = GenerateIPAddress();
                var cvarName = CvarNames[_random.Next(CvarNames.Length)];
                lines.Add($"STEAMID:{steamId} | Player名前中文 | {ip} | Invalid {cvarName}: 1 (KTP value: 0)");
            }

            // Add special characters
            for (int i = 0; i < specialChars; i++)
            {
                var steamId = GenerateSteamID();
                var ip = GenerateIPAddress();
                var cvarName = CvarNames[_random.Next(CvarNames.Length)];
                lines.Add($"STEAMID:{steamId} | Player<>[]{{}}!@# | {ip} | Invalid {cvarName}: 1 (KTP value: 0)");
            }

            // Fill remainder with regular log lines
            int remaining = totalCount - lines.Count;
            for (int i = 0; i < remaining; i++)
            {
                lines.Add(GenerateRegularLogLine());
            }

            // Shuffle for realism
            return lines.OrderBy(x => _random.Next()).ToList();
        }

        /// <summary>
        /// Generate a batch of server logs for multi-server testing
        /// </summary>
        public static Dictionary<string, List<string>> GenerateMultiServerLogs(
            int serverCount,
            int linesPerServer)
        {
            var serverLogs = new Dictionary<string, List<string>>();

            for (int i = 1; i <= serverCount; i++)
            {
                string serverName = $"1911-TEST-{i}";
                serverLogs[serverName] = GenerateLogLines(linesPerServer);
            }

            return serverLogs;
        }
    }
}
