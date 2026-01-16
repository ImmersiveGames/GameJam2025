using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using _ImmersiveGames.NewScripts.QA.Baseline2.Contract;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline2.Verifier
{
    internal static class Baseline21ContractDrivenVerifier
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string ContractFile = "Observability-Contract.md";
        internal const string LogFile = "Baseline-2.1-Smoke-LastRun.log";
        internal const string OutputMdFile = "Baseline-2.1-ContractVerification-LastRun.md";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string ContractAbs => Path.Combine(ReportsDirAbs, ContractFile);
        internal static string LogAbs => Path.Combine(ReportsDirAbs, LogFile);
        internal static string OutputMdAbs => Path.Combine(ReportsDirAbs, OutputMdFile);

        internal enum VerificationStatus
        {
            Pass,
            Fail,
            Inconclusive
        }

        internal sealed class DomainResult
        {
            internal string Name;
            internal VerificationStatus Status;
            internal IReadOnlyList<string> EvidenceFound = Array.Empty<string>();
            internal IReadOnlyList<string> EvidenceMissing = Array.Empty<string>();
        }

        internal sealed class InvariantResult
        {
            internal string Name;
            internal VerificationStatus Status;
            internal string Details;
        }

        internal sealed class VerificationResult
        {
            internal VerificationStatus Status;
            internal string Summary;
            internal IReadOnlyList<DomainResult> Domains = Array.Empty<DomainResult>();
            internal IReadOnlyList<InvariantResult> Invariants = Array.Empty<InvariantResult>();
            internal IReadOnlyList<string> Diagnostics = Array.Empty<string>();
            internal int LogLineCount;
        }

        internal static VerificationResult VerifyLastRun()
        {
            return Verify(ContractAbs, LogAbs);
        }

        internal static VerificationResult VerifyLastRunAndWriteReport()
        {
            var result = VerifyLastRun();
            TryWriteMarkdownReport(result, OutputMdAbs);
            return result;
        }

        internal static VerificationResult Verify(string contractPath, string logPath)
        {
            var result = new VerificationResult();

            var contract = ObservabilityContractParser.Parse(contractPath);
            var diagnostics = new List<string>(contract.Errors);

            if (!File.Exists(logPath))
            {
                diagnostics.Add($"Log not found: {logPath}");
                result.Status = VerificationStatus.Inconclusive;
                result.Diagnostics = diagnostics;
                result.Summary = BuildSummary(result, "Log not found.");
                return result;
            }

            string[] logLines;
            try
            {
                logLines = File.ReadAllLines(logPath);
            }
            catch (Exception ex)
            {
                diagnostics.Add($"Log read failed: {ex.GetType().Name}: {ex.Message}");
                result.Status = VerificationStatus.Inconclusive;
                result.Diagnostics = diagnostics;
                result.Summary = BuildSummary(result, "Log read failed.");
                return result;
            }

            result.LogLineCount = logLines.Length;

            var domainResults = new List<DomainResult>();
            bool anyFail = false;

            foreach (var domain in contract.Domains)
            {
                var found = new List<string>();
                var missing = new List<string>();

                foreach (var token in domain.EvidenceTokens)
                {
                    if (ContainsToken(logLines, token))
                        found.Add(token);
                    else
                        missing.Add(token);
                }

                var status = missing.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail;
                if (status == VerificationStatus.Fail)
                    anyFail = true;

                foreach (var token in missing)
                    AddMissingEvidenceDiagnostic(diagnostics, logLines, domain.Name, token);

                domainResults.Add(new DomainResult
                {
                    Name = domain.Name,
                    Status = domain.EvidenceTokens.Count == 0 ? VerificationStatus.Inconclusive : status,
                    EvidenceFound = found,
                    EvidenceMissing = missing
                });
            }

            var invariants = EvaluateInvariants(logLines);
            foreach (var invariant in invariants)
            {
                if (invariant.Status != VerificationStatus.Pass)
                    diagnostics.Add($"Invariant {invariant.Name}: {invariant.Details}");
            }
            if (invariants.Any(i => i.Status == VerificationStatus.Fail))
                anyFail = true;

            result.Domains = domainResults;
            result.Invariants = invariants;
            result.Diagnostics = diagnostics;

            if (diagnostics.Count > 0)
                result.Status = anyFail ? VerificationStatus.Fail : VerificationStatus.Inconclusive;
            else
                result.Status = anyFail ? VerificationStatus.Fail : VerificationStatus.Pass;

            result.Summary = BuildSummary(result, "Contract-driven verification completed.");
            return result;
        }

        private static List<InvariantResult> EvaluateInvariants(string[] logLines)
        {
            var results = new List<InvariantResult>();

            var scenesReadyIndex = IndexOfContains(logLines, "SceneTransitionScenesReady");
            var completedIndex = IndexOfContains(logLines, "SceneTransitionCompleted");

            var sceneFlowStatus = VerificationStatus.Pass;
            var sceneFlowDetails = "ScenesReady appears before Completed.";

            if (scenesReadyIndex < 0 || completedIndex < 0)
            {
                sceneFlowStatus = VerificationStatus.Inconclusive;
                sceneFlowDetails = "Missing ScenesReady/Completed evidence in log.";
            }
            else if (scenesReadyIndex > completedIndex)
            {
                sceneFlowStatus = VerificationStatus.Fail;
                sceneFlowDetails = "ScenesReady appears after Completed.";
            }

            results.Add(new InvariantResult
            {
                Name = "SceneFlow: ScenesReady before Completed",
                Status = sceneFlowStatus,
                Details = sceneFlowDetails
            });

            var resetCompletedIndex = IndexOfContains(logLines, "WorldLifecycleResetCompletedEvent");
            var resetCompletedStatus = resetCompletedIndex >= 0 ? VerificationStatus.Pass : VerificationStatus.Fail;

            results.Add(new InvariantResult
            {
                Name = "WorldLifecycle: ResetCompleted emitted",
                Status = resetCompletedStatus,
                Details = resetCompletedIndex >= 0 ? "ResetCompleted evidence found." : "ResetCompleted evidence missing."
            });

            return results;
        }

        private static bool ContainsToken(string[] lines, string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static int IndexOfContains(string[] lines, string token)
        {
            if (string.IsNullOrEmpty(token))
                return -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            return -1;
        }

        private static void AddMissingEvidenceDiagnostic(
            List<string> diagnostics,
            string[] logLines,
            string domainName,
            string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            var snippet = FindClosestLineSnippet(logLines, token);
            if (string.IsNullOrEmpty(snippet))
                diagnostics.Add($"Missing evidence `{token}` (domain {domainName}). No close match found.");
            else
                diagnostics.Add($"Missing evidence `{token}` (domain {domainName}). Closest match: {snippet}");
        }

        private static string FindClosestLineSnippet(string[] logLines, string token)
        {
            if (logLines == null || logLines.Length == 0)
                return string.Empty;

            var hint = ExtractTokenHint(token);
            var index = IndexOfContains(logLines, hint);
            if (index < 0)
                return string.Empty;

            var line = logLines[index];
            return $"line {index + 1}: {line}";
        }

        private static string ExtractTokenHint(string token)
        {
            if (string.IsNullOrEmpty(token))
                return string.Empty;

            var separators = new[] { '/', ':', ' ' };
            var split = token.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0)
                return token;

            return split[0].Length < 3 ? token : split[0];
        }

        private static string BuildSummary(VerificationResult result, string note)
        {
            var pass = result.Domains?.Count(d => d.Status == VerificationStatus.Pass) ?? 0;
            var fail = result.Domains?.Count(d => d.Status == VerificationStatus.Fail) ?? 0;
            var inc = result.Domains?.Count(d => d.Status == VerificationStatus.Inconclusive) ?? 0;

            return
                $"Status={result.Status} | Domains={result.Domains?.Count ?? 0} | Pass={pass} | Fail={fail} | Inconclusive={inc} | " +
                $"LogLines={result.LogLineCount} | Note='{note}'";
        }

        internal static bool TryWriteMarkdownReport(VerificationResult result, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ReportsDirAbs);
                var md = BuildMarkdownReport(result);
                File.WriteAllText(outputPath, md, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Baseline21ContractDrivenVerifier] Failed to write report: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static string BuildMarkdownReport(VerificationResult result)
        {
            var sb = new StringBuilder(16 * 1024);
            sb.AppendLine("# Baseline 2.1 — Contract-driven Verification (Last Run)");
            sb.AppendLine();
            sb.AppendLine($"- Date (local): {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Status: **{result.Status}**");
            sb.AppendLine($"- Log lines: {result.LogLineCount}");
            sb.AppendLine();
            sb.AppendLine("## Inputs (paths)");
            sb.AppendLine($"- Contract: `{ContractAbs}`");
            sb.AppendLine($"- Log: `{LogAbs}`");
            sb.AppendLine($"- Output: `{OutputMdAbs}`");
            sb.AppendLine();

            sb.AppendLine("## Diagnostics");
            sb.AppendLine();
            if (result.Diagnostics.Count > 0)
            {
                foreach (var diag in result.Diagnostics)
                    sb.AppendLine($"- {diag}");
            }
            else
            {
                sb.AppendLine("- None.");
            }
            sb.AppendLine();

            sb.AppendLine("## Domain Status");
            sb.AppendLine();
            foreach (var domain in result.Domains)
                sb.AppendLine($"- {domain.Name}: **{domain.Status.ToString().ToUpperInvariant()}**");
            sb.AppendLine();

            sb.AppendLine("## Domain Results");
            sb.AppendLine();
            foreach (var domain in result.Domains)
            {
                sb.AppendLine($"### {domain.Name} — **{domain.Status}**");
                sb.AppendLine();
                sb.AppendLine("**Evidence found**");
                foreach (var token in domain.EvidenceFound)
                    sb.AppendLine($"- `{token}`");
                sb.AppendLine();
                sb.AppendLine("**Evidence missing**");
                foreach (var token in domain.EvidenceMissing)
                    sb.AppendLine($"- `{token}`");
                sb.AppendLine();
            }

            sb.AppendLine("## Invariants");
            sb.AppendLine();
            foreach (var invariant in result.Invariants)
            {
                sb.AppendLine($"- **{invariant.Status}** — {invariant.Name}");
                sb.AppendLine($"  - {invariant.Details}");
            }

            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine(result.Summary ?? string.Empty);

            return sb.ToString();
        }
    }
}
