using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline2ChecklistDrivenVerifier
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string ChecklistFile = "Baseline-2.0-Checklist.md";
        internal const string LogFile = "Baseline-2.0-Smoke-LastRun.log";
        internal const string OutputMdFile = "Baseline-2.0-ChecklistVerification-LastRun.md";

        internal const string EvidenceSectionHeader = "## Evidências hard (log — strings exatas)";
        internal const string FailMarker = "[Baseline][FAIL]";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string ChecklistAbs => Path.Combine(ReportsDirAbs, ChecklistFile);
        internal static string LogAbs => Path.Combine(ReportsDirAbs, LogFile);
        internal static string OutputMdAbs => Path.Combine(ReportsDirAbs, OutputMdFile);

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

            internal string ChecklistPath;
            internal string LogPath;
            internal string OutputReportPath;

            internal int LogLineCount;
            internal int EvidenceBlockCount;
            internal int EvidenceTotalCount;

            internal bool FailMarkerFound;
            internal int FailMarkerLineNumber;
            internal string FailMarkerLine;

            internal IReadOnlyList<BlockResult> Blocks = Array.Empty<BlockResult>();
        }

        internal sealed class BlockResult
        {
            internal string Name;
            internal VerificationStatus Status;
            internal IReadOnlyList<EvidenceResult> Evidence = Array.Empty<EvidenceResult>();

            internal int EvidenceCount;
            internal int FoundCount;
            internal int MissingCount;
        }

        internal sealed class EvidenceResult
        {
            internal string RawText;
            internal bool Found;
            internal int LineNumber;     // 1-based
            internal string Snippet;     // original line (non-normalized), truncated
        }

        internal static VerificationResult VerifyLastRun()
        {
            return Verify(ChecklistAbs, LogAbs);
        }

        /// <summary>
        /// Verifica e também grava um relatório Markdown canônico em Docs/Reports.
        /// Não altera o tool grande; gera um arquivo separado.
        /// </summary>
        internal static VerificationResult VerifyLastRunAndWriteReport(bool includeFoundEvidenceInMd = false)
        {
            var result = VerifyLastRun();
            TryWriteMarkdownReport(result, OutputMdAbs, includeFoundEvidenceInMd);
            return result;
        }

        internal static VerificationResult Verify(string checklistPath, string logPath)
        {
            var result = new VerificationResult
            {
                ChecklistPath = checklistPath,
                LogPath = logPath,
                OutputReportPath = OutputMdAbs
            };

            if (!File.Exists(checklistPath))
            {
                result.Status = VerificationStatus.Inconclusive;
                result.Summary = BuildSummary(result, "Checklist not found.");
                return result;
            }

            if (!File.Exists(logPath))
            {
                result.Status = VerificationStatus.Inconclusive;
                result.Summary = BuildSummary(result, "Log not found.");
                return result;
            }

            string[] checklistLines;
            try
            {
                checklistLines = File.ReadAllLines(checklistPath);
            }
            catch (Exception ex)
            {
                result.Status = VerificationStatus.Inconclusive;
                result.Summary = BuildSummary(result, $"Checklist read failed: {ex.GetType().Name}: {ex.Message}");
                return result;
            }

            var blocks = ParseEvidenceBlocks(checklistLines);
            if (blocks.Count == 0)
            {
                result.Status = VerificationStatus.Inconclusive;
                result.Summary = BuildSummary(result, "Evidence section not found or empty.");
                return result;
            }

            string[] logLinesRaw;
            try
            {
                logLinesRaw = File.ReadAllLines(logPath);
            }
            catch (Exception ex)
            {
                result.Status = VerificationStatus.Inconclusive;
                result.Summary = BuildSummary(result, $"Log read failed: {ex.GetType().Name}: {ex.Message}");
                return result;
            }

            result.LogLineCount = logLinesRaw.Length;

            var normalizedLogLines = NormalizeLogLines(logLinesRaw);

            var failMarker = FindFailMarker(normalizedLogLines);
            result.FailMarkerFound = failMarker.Found;
            result.FailMarkerLineNumber = failMarker.LineNumber;
            result.FailMarkerLine = failMarker.Line;

            var blockResults = new List<BlockResult>(blocks.Count);
            bool anyBlockFail = false;

            int evidenceTotal = 0;

            foreach (var block in blocks)
            {
                var evidenceResults = new List<EvidenceResult>(block.Evidence.Count);
                bool blockFail = false;

                foreach (var rawEvidence in block.Evidence)
                {
                    evidenceTotal++;

                    var pattern = BuildEvidenceRegex(rawEvidence);
                    var match = FindFirstMatch(normalizedLogLines, pattern);

                    if (!match.Found)
                        blockFail = true;

                    evidenceResults.Add(new EvidenceResult
                    {
                        RawText = rawEvidence,
                        Found = match.Found,
                        LineNumber = match.LineNumber,
                        Snippet = Truncate(match.Snippet, 260)
                    });
                }

                var foundCount = evidenceResults.Count(e => e.Found);
                var missingCount = evidenceResults.Count - foundCount;

                var blockStatus = blockFail ? VerificationStatus.Fail : VerificationStatus.Pass;
                if (blockFail)
                    anyBlockFail = true;

                blockResults.Add(new BlockResult
                {
                    Name = block.Name,
                    Status = blockStatus,
                    Evidence = evidenceResults,
                    EvidenceCount = evidenceResults.Count,
                    FoundCount = foundCount,
                    MissingCount = missingCount
                });
            }

            result.EvidenceBlockCount = blocks.Count;
            result.EvidenceTotalCount = evidenceTotal;
            result.Blocks = blockResults;

            var overallStatus = VerificationStatus.Pass;
            if (result.FailMarkerFound || anyBlockFail)
                overallStatus = VerificationStatus.Fail;

            result.Status = overallStatus;
            result.Summary = BuildSummary(result, anyBlockFail ? "One or more blocks failed." : "All blocks passed.");

            return result;
        }

        private static string BuildSummary(VerificationResult r, string note)
        {
            // Mantém o summary curto e sempre com os paths para evitar “arquivo errado”.
            var pass = r.Blocks?.Count(b => b.Status == VerificationStatus.Pass) ?? 0;
            var fail = r.Blocks?.Count(b => b.Status == VerificationStatus.Fail) ?? 0;
            var inc = r.Blocks?.Count(b => b.Status == VerificationStatus.Inconclusive) ?? 0;

            return
                $"Status={r.Status} | Blocks={r.EvidenceBlockCount} | Pass={pass} | Fail={fail} | Inconclusive={inc} | " +
                $"FailMarkerFound={r.FailMarkerFound} | LogLines={r.LogLineCount} | Evidence={r.EvidenceTotalCount} | " +
                $"Checklist='{r.ChecklistPath}' | Log='{r.LogPath}' | Out='{r.OutputReportPath}' | Note='{note}'";
        }

        private static (int LineNumber, string Snippet, bool Found) FindFirstMatch(IReadOnlyList<NormalizedLine> lines, Regex pattern)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (pattern.IsMatch(lines[i].Text))
                {
                    // Snippet deve ser a linha original para o usuário copiar/colar.
                    return (lines[i].Number, lines[i].Original, true);
                }
            }

            return (0, string.Empty, false);
        }

        private static (bool Found, int LineNumber, string Line) FindFailMarker(IReadOnlyList<NormalizedLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Text.IndexOf(FailMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return (true, lines[i].Number, lines[i].Original);
            }

            return (false, 0, string.Empty);
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
                var raw = lines[i] ?? string.Empty;
                var line = raw.TrimEnd();

                // Ex.: - **Boot → Menu (startup)**
                if (line.StartsWith("- **", StringComparison.Ordinal) && line.EndsWith("**", StringComparison.Ordinal))
                {
                    var name = line.Substring(4, line.Length - 6).Trim();
                    current = new EvidenceBlock { Name = name };
                    blocks.Add(current);
                    continue;
                }

                if (current == null)
                    continue;

                // Evidência canônica = conteúdo entre backticks.
                var evidence = ExtractBacktickContent(line);
                if (!string.IsNullOrEmpty(evidence))
                    current.Evidence.Add(evidence);
            }

            return blocks;
        }

        private static string ExtractBacktickContent(string line)
        {
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
            // Regras:
            // - "..." vira wildcard
            // - espaços viram \s+
            // - ' e " viram (['"])
            // - "→" vira (?:→|->)
            // - match "contém" (não precisa ser linha inteira)
            //
            // Observação: rodamos em cima de uma linha normalizada (sem <color> e sem sufixo de timing).

            if (string.IsNullOrEmpty(rawText))
                return new Regex("a^", RegexOptions.Compiled); // nunca casa

            var sb = new StringBuilder(rawText.Length * 2);

            // prefix "contains"
            sb.Append(".*");

            for (int i = 0; i < rawText.Length; i++)
            {
                // wildcard "..."
                if (i + 2 < rawText.Length &&
                    rawText[i] == '.' &&
                    rawText[i + 1] == '.' &&
                    rawText[i + 2] == '.')
                {
                    sb.Append(".*");
                    i += 2;
                    continue;
                }

                char c = rawText[i];

                // whitespace tolerant
                if (char.IsWhiteSpace(c))
                {
                    sb.Append("\\s+");
                    continue;
                }

                // tolerate single/double quotes
                if (c == '\'' || c == '"')
                {
                    sb.Append("['\\\"]");
                    continue;
                }

                // tolerate arrow variants
                if (c == '→')
                {
                    sb.Append("(?:→|->)");
                    continue;
                }

                // escape regex meta
                sb.Append(EscapeRegexChar(c));
            }

            sb.Append(".*");

            return new Regex(
                sb.ToString(),
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private static string EscapeRegexChar(char c)
        {
            // Escapa apenas metacharacters do regex.
            switch (c)
            {
                case '\\':
                case '.':
                case '$':
                case '^':
                case '{':
                case '[':
                case '(':
                case '|':
                case ')':
                case '*':
                case '+':
                case '?':
                case '<':
                case '>':
                case '-':
                case ']':
                case '}':
                    return "\\" + c;
                default:
                    return c.ToString();
            }
        }

        private static List<NormalizedLine> NormalizeLogLines(string[] lines)
        {
            var result = new List<NormalizedLine>(lines.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i] ?? string.Empty;

                // Mantém Text como normalizado e Original para snippet/cópia.
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
            // Remove sufixo do tipo: " (@ 9,97s)" / " (@ 9.97s)"
            return Regex.Replace(line, "\\s*\\(@\\s*[^)]*s\\)\\s*$", string.Empty);
        }

        internal static bool TryWriteMarkdownReport(VerificationResult result, string outputPath, bool includeFoundEvidence)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ReportsDirAbs);

                var md = BuildMarkdownReport(result, includeFoundEvidence);
                File.WriteAllText(outputPath, md, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Baseline2ChecklistDrivenVerifier] Failed to write report: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static string BuildMarkdownReport(VerificationResult r, bool includeFoundEvidence)
        {
            var sb = new StringBuilder(16 * 1024);

            sb.AppendLine("# Baseline 2.0 — Checklist-driven Verification (Last Run)");
            sb.AppendLine();
            sb.AppendLine($"- Date (local): {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Status: **{r.Status}**");
            sb.AppendLine($"- Fail marker: {(r.FailMarkerFound ? $"FOUND (line {r.FailMarkerLineNumber})" : "NOT FOUND")}");
            sb.AppendLine($"- Blocks: {r.EvidenceBlockCount}");
            sb.AppendLine($"- Evidence total: {r.EvidenceTotalCount}");
            sb.AppendLine($"- Log lines: {r.LogLineCount}");
            sb.AppendLine();
            sb.AppendLine("## Inputs (paths)");
            sb.AppendLine($"- Checklist: `{r.ChecklistPath}`");
            sb.AppendLine($"- Log: `{r.LogPath}`");
            sb.AppendLine($"- Output: `{r.OutputReportPath}`");
            sb.AppendLine();

            if (r.FailMarkerFound)
            {
                sb.AppendLine("## FAIL marker");
                sb.AppendLine();
                sb.AppendLine($"- Line: {r.FailMarkerLineNumber}");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(r.FailMarkerLine ?? string.Empty);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("## Blocks");
            sb.AppendLine();

            foreach (var block in r.Blocks)
            {
                sb.AppendLine($"### {block.Name} — **{block.Status}**");
                sb.AppendLine();
                sb.AppendLine($"- Evidence: {block.EvidenceCount} | Found: {block.FoundCount} | Missing: {block.MissingCount}");
                sb.AppendLine();

                // Por padrão, relatório foca em faltantes (reduz ruído).
                foreach (var ev in block.Evidence)
                {
                    if (!includeFoundEvidence && ev.Found)
                        continue;

                    var status = ev.Found ? "FOUND" : "NOT FOUND";
                    var lineInfo = ev.Found ? $"line={ev.LineNumber}" : "line=--";
                    sb.AppendLine($"- {status} | {lineInfo} | `{ev.RawText}`");

                    if (ev.Found && !string.IsNullOrEmpty(ev.Snippet))
                    {
                        sb.AppendLine();
                        sb.AppendLine("```");
                        sb.AppendLine(ev.Snippet);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            if (text.Length <= maxLen)
                return text;
            return text.Substring(0, maxLen) + "...";
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
