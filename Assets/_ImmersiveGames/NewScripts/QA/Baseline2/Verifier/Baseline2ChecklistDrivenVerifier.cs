using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline2ChecklistDrivenVerifier
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string ChecklistFile = "Baseline-2.0-Checklist.md";
        internal const string LogFile = "Baseline-2.0-Smoke-LastRun.log";
        internal const string EvidenceSectionHeader = "## Evidências hard (log — strings exatas)";
        internal const string FailMarker = "[Baseline][FAIL]";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string ChecklistAbs => Path.Combine(ReportsDirAbs, ChecklistFile);
        internal static string LogAbs => Path.Combine(ReportsDirAbs, LogFile);

        internal enum VerificationStatus
        {
            Pass,
            Fail,
            Inconclusive
        }

        internal sealed class VerificationResult
        {
            internal VerificationStatus Status;
            internal string Summary;
            internal bool FailMarkerFound;
            internal string FailMarkerLine;
            internal IReadOnlyList<BlockResult> Blocks;
        }

        internal sealed class BlockResult
        {
            internal string Name;
            internal VerificationStatus Status;
            internal IReadOnlyList<EvidenceResult> Evidence;
        }

        internal sealed class EvidenceResult
        {
            internal string RawText;
            internal bool Found;
            internal int LineNumber;
            internal string Snippet;
        }

        internal static VerificationResult VerifyLastRun()
        {
            return Verify(ChecklistAbs, LogAbs);
        }

        internal static VerificationResult Verify(string checklistPath, string logPath)
        {
            if (!File.Exists(checklistPath))
            {
                return new VerificationResult
                {
                    Status = VerificationStatus.Inconclusive,
                    Summary = $"Checklist not found: {checklistPath}",
                    Blocks = Array.Empty<BlockResult>()
                };
            }

            if (!File.Exists(logPath))
            {
                return new VerificationResult
                {
                    Status = VerificationStatus.Inconclusive,
                    Summary = $"Log not found: {logPath}",
                    Blocks = Array.Empty<BlockResult>()
                };
            }

            var checklistLines = File.ReadAllLines(checklistPath);
            var blocks = ParseEvidenceBlocks(checklistLines);
            if (blocks.Count == 0)
            {
                return new VerificationResult
                {
                    Status = VerificationStatus.Inconclusive,
                    Summary = "Evidence section not found or empty.",
                    Blocks = Array.Empty<BlockResult>()
                };
            }

            var logLinesRaw = File.ReadAllLines(logPath);
            var normalizedLogLines = NormalizeLogLines(logLinesRaw);

            var failMarker = FindFailMarker(normalizedLogLines);
            bool failMarkerFound = !string.IsNullOrEmpty(failMarker.Line);

            var blockResults = new List<BlockResult>();
            bool anyBlockFail = false;

            foreach (var block in blocks)
            {
                var evidenceResults = new List<EvidenceResult>();
                bool blockFail = false;

                foreach (var rawEvidence in block.Evidence)
                {
                    var pattern = BuildEvidenceRegex(rawEvidence);
                    var match = FindFirstMatch(normalizedLogLines, pattern);

                    if (!match.Found)
                        blockFail = true;

                    evidenceResults.Add(new EvidenceResult
                    {
                        RawText = rawEvidence,
                        Found = match.Found,
                        LineNumber = match.LineNumber,
                        Snippet = match.Snippet
                    });
                }

                var blockStatus = blockFail ? VerificationStatus.Fail : VerificationStatus.Pass;
                if (blockFail)
                    anyBlockFail = true;

                blockResults.Add(new BlockResult
                {
                    Name = block.Name,
                    Status = blockStatus,
                    Evidence = evidenceResults
                });
            }

            var overallStatus = VerificationStatus.Pass;
            if (failMarkerFound || anyBlockFail)
                overallStatus = VerificationStatus.Fail;

            return new VerificationResult
            {
                Status = overallStatus,
                Summary = BuildSummary(overallStatus, blockResults, failMarkerFound, failMarker.Line),
                FailMarkerFound = failMarkerFound,
                FailMarkerLine = failMarker.Line,
                Blocks = blockResults
            };
        }

        private static string BuildSummary(VerificationStatus overall, List<BlockResult> blocks, bool failMarkerFound, string failMarkerLine)
        {
            var lines = new List<string>
            {
                $"Status: {overall}",
                $"Blocks: {blocks.Count}",
                $"FailMarkerFound: {failMarkerFound}"
            };

            if (failMarkerFound)
                lines.Add($"FailMarkerLine: {failMarkerLine}");

            int pass = blocks.Count(b => b.Status == VerificationStatus.Pass);
            int fail = blocks.Count(b => b.Status == VerificationStatus.Fail);
            int inconclusive = blocks.Count(b => b.Status == VerificationStatus.Inconclusive);

            lines.Add($"BlockStatus: Pass={pass}, Fail={fail}, Inconclusive={inconclusive}");
            return string.Join(" | ", lines);
        }

        private static (int LineNumber, string Line, string Snippet, bool Found) FindFirstMatch(IReadOnlyList<NormalizedLine> lines, Regex pattern)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (pattern.IsMatch(lines[i].Text))
                {
                    return (lines[i].Number, lines[i].Text, lines[i].Original, true);
                }
            }

            return (0, string.Empty, string.Empty, false);
        }

        private static (string Line, int LineNumber) FindFailMarker(IReadOnlyList<NormalizedLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Text.Contains(FailMarker, StringComparison.Ordinal))
                    return (lines[i].Original, lines[i].Number);
            }

            return (string.Empty, 0);
        }

        private static List<EvidenceBlock> ParseEvidenceBlocks(string[] lines)
        {
            var blocks = new List<EvidenceBlock>();

            int headerIndex = Array.FindIndex(lines, line => line.Trim() == EvidenceSectionHeader);
            if (headerIndex < 0)
                return blocks;

            int endIndex = lines.Length;
            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal))
                {
                    endIndex = i;
                    break;
                }
            }

            EvidenceBlock current = null;
            for (int i = headerIndex + 1; i < endIndex; i++)
            {
                var line = lines[i].TrimEnd();

                if (line.StartsWith("- **", StringComparison.Ordinal) && line.EndsWith("**", StringComparison.Ordinal))
                {
                    var name = line.Substring(4, line.Length - 6).Trim();
                    current = new EvidenceBlock { Name = name };
                    blocks.Add(current);
                    continue;
                }

                if (current == null)
                    continue;

                var evidence = ExtractBacktickContent(line);
                if (!string.IsNullOrEmpty(evidence))
                    current.Evidence.Add(evidence);
            }

            return blocks;
        }

        private static string ExtractBacktickContent(string line)
        {
            // Usa o conteúdo entre backticks como string canônica.
            int start = line.IndexOf('`');
            if (start < 0)
                return string.Empty;

            int end = line.IndexOf('`', start + 1);
            if (end < 0)
                return string.Empty;

            return line.Substring(start + 1, end - start - 1).Trim();
        }

        private static Regex BuildEvidenceRegex(string rawText)
        {
            // Constrói um padrão robusto: "..." vira wildcard, espaço vira \s+.
            var escaped = Regex.Escape(rawText);
            escaped = escaped.Replace("\\.\\.\\.", ".*");
            escaped = Regex.Replace(escaped, "\\s+", "\\\\s+");

            var fullPattern = $".*{escaped}.*";
            return new Regex(fullPattern, RegexOptions.Compiled);
        }

        private static List<NormalizedLine> NormalizeLogLines(string[] lines)
        {
            var result = new List<NormalizedLine>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i] ?? string.Empty;
                var normalized = raw.Replace("\r", string.Empty);
                normalized = StripColorTags(normalized);
                normalized = StripTimingSuffix(normalized);
                result.Add(new NormalizedLine
                {
                    Number = i + 1,
                    Text = normalized,
                    Original = raw
                });
            }

            return result;
        }

        private static string StripColorTags(string line)
        {
            return Regex.Replace(line, "</?color[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        }

        private static string StripTimingSuffix(string line)
        {
            // Remove sufixo "(@ 9,97s)" para tolerar variações de tempo.
            return Regex.Replace(line, "\\s*\\(@\\s*[^)]*s\\)\\s*$", string.Empty);
        }

        private sealed class EvidenceBlock
        {
            internal string Name;
            internal List<string> Evidence = new List<string>();
        }

        private struct NormalizedLine
        {
            internal int Number;
            internal string Text;
            internal string Original;
        }
    }
}
