// Baseline2SmokeLastRunTool.cs
// Place under an Editor folder, e.g.:
// Assets/_ImmersiveGames/NewScripts/Editor/Baseline2/Baseline2SmokeLastRunTool.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.EditorTools.Baseline2
{
    /// <summary>
    /// Single-entry Baseline 2.0 Smoke capture + report generator.
    ///
    /// - One menu item only (toggle).
    /// - Arm in Edit Mode, press Play, run tests, press Stop.
    /// - On Stop, writes:
    ///     Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log
    ///     Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md
    ///
    /// No clipboard / no "from file" options. Always from the captured .log.
    /// </summary>
    [InitializeOnLoad]
    public static class Baseline2SmokeLastRunTool
    {
        // =========================
        // Menu (ONE item)
        // =========================
        private const string MenuPath = "Tools/NewScripts/Baseline 2.0/Smoke Last Run (Toggle Capture + Auto Report)";

        // =========================
        // Persistent keys
        // =========================
        private const string PrefEnabled = "Baseline2.Smoke.Enabled";
        private const string PrefPendingStart = "Baseline2.Smoke.PendingStart";
        private const string PrefReportPending = "Baseline2.Smoke.ReportPending";

        // =========================
        // Output paths
        // =========================
        private const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        private const string LastRunLogFile = "Baseline-2.0-Smoke-LastRun.log";
        private const string LastRunMdFile = "Baseline-2.0-Smoke-LastRun.md";
        private const string SpecFile = "Baseline-2.0-Spec.md";

        private static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        private static string LastRunLogAbs => Path.Combine(ReportsDirAbs, LastRunLogFile);
        private static string LastRunMdAbs => Path.Combine(ReportsDirAbs, LastRunMdFile);
        private static string SpecAbs => Path.Combine(ReportsDirAbs, SpecFile);

        // =========================
        // Capture state (in-memory)
        // =========================
        private static bool _capturing;
        private static DateTime _captureStartUtc;
        private static StreamWriter _writer;
        private static readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private static readonly object _writerLock = new object();

        static Baseline2SmokeLastRunTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;

            // If we were "pending start" and the domain reloaded into Play, start immediately.
            TryStartIfPendingAndInPlay();
            TryGenerateReportIfPending();
        }

        [MenuItem(MenuPath)]
        private static void ToggleEnabled()
        {
            if (_capturing)
            {
                StopCaptureAndScheduleReport();
                EditorPrefs.SetBool(PrefEnabled, false);
                EditorPrefs.SetBool(PrefPendingStart, false);
                Debug.Log("[Baseline2Smoke] Capture STOP requested. Report will be generated on Stop.");
                return;
            }

            bool enabled = EditorPrefs.GetBool(PrefEnabled, false);
            enabled = !enabled;
            EditorPrefs.SetBool(PrefEnabled, enabled);

            if (enabled)
            {
                EditorPrefs.SetBool(PrefPendingStart, true);
                Debug.Log("[Baseline2Smoke] Capture ARMED. Press Play. Report will be generated automatically on Stop.");

                if (EditorApplication.isPlaying)
                    TryStartIfPendingAndInPlay(force: true);
            }
            else
            {
                EditorPrefs.SetBool(PrefPendingStart, false);
                Debug.Log("[Baseline2Smoke] Capture DISARMED.");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            bool enabled = EditorPrefs.GetBool(PrefEnabled, false);
            Menu.SetChecked(MenuPath, enabled || _capturing);
            return true;
        }

        // =========================
        // Play mode hooks
        // =========================
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PrefEnabled, false))
            {
                // If user disarmed while capturing, stop cleanly.
                if (_capturing)
                    StopCaptureAndScheduleReport();
                return;
            }

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // Mark pending start before the Play transition.
                    EditorPrefs.SetBool(PrefPendingStart, true);
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    // Fallback start if we didn't catch via pending start.
                    TryStartIfPendingAndInPlay(force: true);
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // Stop capture before we fully return to edit mode.
                    if (_capturing)
                        StopCaptureAndScheduleReport();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Generate report after returning to edit mode.
                    TryGenerateReportIfPending();
                    break;
            }
        }

        private static void TryStartIfPendingAndInPlay(bool force = false)
        {
            if (_capturing)
                return;

            bool pending = EditorPrefs.GetBool(PrefPendingStart, false);

            if (!pending && !force)
                return;

            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorPrefs.SetBool(PrefPendingStart, false);
            StartCapture();
        }

        // =========================
        // Capture implementation
        // =========================
        private static void StartCapture()
        {
            try
            {
                Directory.CreateDirectory(ReportsDirAbs);

                // Overwrite last run file.
                _writer = new StreamWriter(
                    LastRunLogAbs,
                    append: false,
                    encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                )
                { AutoFlush = false };

                _capturing = true;
                _captureStartUtc = DateTime.UtcNow;

                Application.logMessageReceivedThreaded += OnLogThreaded;

                WriteLine($"[Baseline2Smoke] CAPTURE STARTED. utc={_captureStartUtc:O}");
                WriteLine($"[Baseline2Smoke] Output: {LastRunLogAbs}");
                WriteLine("------------------------------------------------------------");

                Debug.Log($"[Baseline2Smoke] CAPTURE STARTED -> {LastRunLogAbs}");
            }
            catch (Exception ex)
            {
                _capturing = false;
                SafeCloseWriter();
                Debug.LogError($"[Baseline2Smoke] Failed to start capture: {ex}");
            }
        }

        private static void StopCaptureAndScheduleReport()
        {
            try
            {
                Application.logMessageReceivedThreaded -= OnLogThreaded;

                var endUtc = DateTime.UtcNow;
                var duration = endUtc - _captureStartUtc;

                WriteLine("------------------------------------------------------------");
                WriteLine($"[Baseline2Smoke] CAPTURE STOPPED. utc={endUtc:O} duration={duration.TotalSeconds:F2}s");

                FlushQueueToDisk();
                SafeCloseWriter();

                _capturing = false;

                // Generate report on EnteredEditMode (or immediately if already in edit mode).
                EditorPrefs.SetBool(PrefReportPending, true);
                EditorPrefs.SetBool(PrefEnabled, false);
                EditorPrefs.SetBool(PrefPendingStart, false);

                Debug.Log($"[Baseline2Smoke] CAPTURE STOPPED. Scheduling report generation -> {LastRunMdAbs}");
            }
            catch (Exception ex)
            {
                _capturing = false;
                SafeCloseWriter();
                Debug.LogError($"[Baseline2Smoke] Failed to stop capture: {ex}");
            }
        }

        private static void OnLogThreaded(string condition, string stackTrace, LogType type)
        {
            // Keep full message to avoid breaking evidence patterns.
            string line;

            if (type == LogType.Exception || type == LogType.Error)
            {
                if (!string.IsNullOrEmpty(stackTrace))
                    line = $"{condition}\n{stackTrace}";
                else
                    line = condition;
            }
            else
            {
                line = condition;
            }

            // Normalize line endings.
            line = line.Replace("\r\n", "\n").Replace("\r", "\n");

            // Split multi-line into individual lines.
            foreach (var l in line.Split('\n'))
            {
                if (!string.IsNullOrEmpty(l))
                    _queue.Enqueue(l);
            }
        }

        private static void OnEditorUpdate()
        {
            if (!_capturing)
                return;

            // Periodic flush to reduce loss risk.
            FlushQueueToDisk();
        }

        private static void FlushQueueToDisk()
        {
            if (_writer == null)
                return;

            lock (_writerLock)
            {
                while (_queue.TryDequeue(out var line))
                    _writer.WriteLine(line);

                _writer.Flush();
            }
        }

        private static void WriteLine(string line)
        {
            if (_writer == null)
                return;

            lock (_writerLock)
            {
                _writer.WriteLine(line);
                _writer.Flush();
            }
        }

        private static void SafeCloseWriter()
        {
            try
            {
                lock (_writerLock)
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
            }
            catch
            {
                // ignored
            }
        }

        // =========================
        // Report generation
        // =========================
        private static void TryGenerateReportIfPending()
        {
            if (!EditorPrefs.GetBool(PrefReportPending, false))
                return;

            // Only generate in edit mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorPrefs.SetBool(PrefReportPending, false);

            try
            {
                if (!File.Exists(LastRunLogAbs))
                {
                    Debug.LogWarning($"[Baseline2Smoke] Report generation skipped: log not found -> {LastRunLogAbs}");
                    return;
                }

                var lines = File.ReadAllLines(LastRunLogAbs);

                var report = GenerateMarkdownReport(lines, LastRunLogAbs);

                Directory.CreateDirectory(ReportsDirAbs);
                File.WriteAllText(LastRunMdAbs, report, new UTF8Encoding(false));

                AssetDatabase.Refresh();

                Debug.Log($"[Baseline2Smoke] Report generated -> {LastRunMdAbs}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Baseline2Smoke] Report generation failed: {ex}");
            }
        }

        private static string GenerateMarkdownReport(string[] lines, string sourcePath)
        {
            var generatedAtUtc = DateTime.UtcNow;

            var tokenStats = ComputeTokenStats(lines);
            var tokenImbalances = tokenStats
                .Where(kv => kv.Value.Acquire != kv.Value.Release)
                .OrderBy(kv => kv.Key)
                .ToList();

            var spec = LoadSpec(SpecAbs);
            var scenarioResults = spec.Scenarios.Select(s => EvaluateScenario(lines, s)).ToList();
            var invariantResult = EvaluateInvariants(lines, spec);

            bool anyScenarioFail = scenarioResults.Any(r => r.Result == "FAIL");
            bool tokenFail = tokenImbalances.Count > 0;
            bool invariantFail = invariantResult.Result == "FAIL";
            bool specFail = spec.Errors.Count > 0 || spec.Scenarios.Count == 0;

            string overall = (anyScenarioFail || tokenFail || invariantFail || specFail) ? "FAIL" : "PASS";

            var sb = new StringBuilder(16 * 1024);

            sb.AppendLine("# Baseline 2.0 — Last Run Report");
            sb.AppendLine();
            sb.AppendLine($"- GeneratedAt: `{generatedAtUtc:yyyy-MM-dd HH:mm:ss}Z`");
            sb.AppendLine($"- Source: `{sourcePath}`");
            sb.AppendLine($"- Spec: `{SpecAbs}`");
            sb.AppendLine($"- Result: **{overall}**");
            sb.AppendLine();

            sb.AppendLine("## Scenario Results");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Title | Result | Missing (Hard) | Order Violations |");
            sb.AppendLine("|---|---|---:|---:|---:|");

            foreach (var r in scenarioResults)
                sb.AppendLine($"| {r.Id} | {r.Title} | **{r.Result}** | {r.MissingHard.Count} | {r.OrderViolations.Count} |");

            sb.AppendLine();
            sb.AppendLine("## Global Invariants");
            sb.AppendLine();
            sb.AppendLine("| Result | Missing (Hard) | Order Violations |");
            sb.AppendLine("|---:|---:|---:|");
            sb.AppendLine($"| **{invariantResult.Result}** | {invariantResult.MissingHard.Count} | {invariantResult.OrderViolations.Count} |");
            sb.AppendLine();

            if (overall == "FAIL")
            {
                sb.AppendLine("## Fail Reasons");
                sb.AppendLine();

                if (specFail)
                {
                    sb.AppendLine("### Spec errors");
                    if (spec.Errors.Count == 0)
                        sb.AppendLine("- Spec did not load any scenario definitions.");
                    else
                        foreach (var error in spec.Errors)
                            sb.AppendLine($"- {error}");
                    sb.AppendLine();
                }

                if (tokenFail)
                {
                    sb.AppendLine("### Token imbalance");
                    foreach (var t in tokenImbalances)
                        sb.AppendLine($"- `{t.Key} (Acquire={t.Value.Acquire}, Release={t.Value.Release})`");
                    sb.AppendLine();
                }

                if (invariantFail)
                {
                    sb.AppendLine("### Global invariants");
                    sb.AppendLine($"- missingHard={invariantResult.MissingHard.Count}, orderViolations={invariantResult.OrderViolations.Count}");
                    sb.AppendLine();
                }

                var failingScenarios = scenarioResults.Where(r => r.Result == "FAIL").ToList();
                if (failingScenarios.Count > 0)
                {
                    sb.AppendLine("### Scenario failures");
                    foreach (var fs in failingScenarios)
                        sb.AppendLine($"- `{fs.Id}`: missingHard={fs.MissingHard.Count}, orderViolations={fs.OrderViolations.Count}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("## Token Summary (Acquire/Release)");
            sb.AppendLine();
            sb.AppendLine("| Token | Acquire | Release |");
            sb.AppendLine("|---|---:|---:|");

            foreach (var kv in tokenStats.OrderBy(k => k.Key))
                sb.AppendLine($"| `{kv.Key}` | {kv.Value.Acquire} | {kv.Value.Release} |");

            sb.AppendLine();
            sb.AppendLine("## Details");
            sb.AppendLine();

            foreach (var r in scenarioResults)
            {
                sb.AppendLine($"### Scenario {r.Id} — {r.Result}");
                sb.AppendLine();
                sb.AppendLine(r.Title);
                sb.AppendLine();

                if (r.MissingHard.Count == 0 && r.MissingSoft.Count == 0 && r.OrderViolations.Count == 0)
                {
                    sb.AppendLine("No issues detected.");
                    sb.AppendLine();
                    continue;
                }

                if (r.MissingHard.Count > 0)
                {
                    sb.AppendLine("**Missing evidence (hard):**");
                    foreach (var m in r.MissingHard)
                        sb.AppendLine($"- {m}");
                    sb.AppendLine();
                }

                if (r.MissingSoft.Count > 0)
                {
                    sb.AppendLine("**Missing evidence (soft):**");
                    foreach (var m in r.MissingSoft)
                        sb.AppendLine($"- {m}");
                    sb.AppendLine();
                }

                if (r.OrderViolations.Count > 0)
                {
                    sb.AppendLine("**Order violations:**");
                    foreach (var v in r.OrderViolations)
                        sb.AppendLine($"- {v}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("### Global Invariants");
            sb.AppendLine();

            if (invariantResult.MissingHard.Count == 0 && invariantResult.OrderViolations.Count == 0)
            {
                sb.AppendLine("No issues detected.");
                sb.AppendLine();
                return sb.ToString();
            }

            if (invariantResult.MissingHard.Count > 0)
            {
                sb.AppendLine("**Missing evidence (hard):**");
                foreach (var m in invariantResult.MissingHard)
                    sb.AppendLine($"- {m}");
                sb.AppendLine();
            }

            if (invariantResult.OrderViolations.Count > 0)
            {
                sb.AppendLine("**Order violations:**");
                foreach (var v in invariantResult.OrderViolations)
                    sb.AppendLine($"- {v}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private struct TokenCounter
        {
            public int Acquire;
            public int Release;
        }

        private static Dictionary<string, TokenCounter> ComputeTokenStats(string[] lines)
        {
            var acq = new Regex(@"Acquire\s+token='([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var rel = new Regex(@"Release\s+token='([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var dict = new Dictionary<string, TokenCounter>();

            foreach (var line in lines)
            {
                var ma = acq.Match(line);
                if (ma.Success)
                {
                    var key = ma.Groups[1].Value;
                    if (!dict.TryGetValue(key, out var c)) c = new TokenCounter();
                    c.Acquire++;
                    dict[key] = c;
                }

                var mr = rel.Match(line);
                if (mr.Success)
                {
                    var key = mr.Groups[1].Value;
                    if (!dict.TryGetValue(key, out var c)) c = new TokenCounter();
                    c.Release++;
                    dict[key] = c;
                }
            }

            return dict;
        }

        private sealed class ScenarioDefinition
        {
            public string Id;
            public string Title;
            public (string Key, Regex Pattern)[] Hard;
            public (string Key, Regex Pattern)[] Soft;
            public (string Key, Regex Before, Regex After)[] OrderRules;
        }

        private sealed class SpecData
        {
            public List<ScenarioDefinition> Scenarios = new List<ScenarioDefinition>();
            public List<(string Key, Regex Pattern)> GlobalHard = new List<(string, Regex)>();
            public List<(string Key, Regex Before, Regex After)> GlobalOrderRules = new List<(string, Regex, Regex)>();
            public List<string> Errors = new List<string>();
        }

        private sealed class ScenarioResult
        {
            public string Id;
            public string Title;
            public string Result; // PASS/FAIL
            public List<string> MissingHard = new List<string>();
            public List<string> MissingSoft = new List<string>();
            public List<string> OrderViolations = new List<string>();
        }

        private sealed class InvariantResult
        {
            public string Result; // PASS/FAIL
            public List<string> MissingHard = new List<string>();
            public List<string> OrderViolations = new List<string>();
        }

        private enum SpecSection
        {
            None,
            Hard,
            Soft,
            Order
        }

        private static SpecData LoadSpec(string specPath)
        {
            var spec = new SpecData();

            if (!File.Exists(specPath))
            {
                spec.Errors.Add($"Spec not found: {specPath}");
                return spec;
            }

            var opt = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            var scenarioHeader = new Regex(@"^###\s+(?:Cen[aá]rio|Scenario)\s+([A-E])\s+—\s+(.+)$", opt);
            var evidenceLine = new Regex(@"^-\s+`([^`]+)`\s*::\s*`([^`]+)`\s*$", opt);
            var orderLine = new Regex(@"^-\s+`([^`]+)`\s*::\s*`([^`]+)`\s*=>\s*`([^`]+)`\s*$", opt);

            ScenarioDefinition currentScenario = null;
            bool inGlobalInvariants = false;
            SpecSection section = SpecSection.None;

            foreach (var rawLine in File.ReadAllLines(specPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
                {
                    inGlobalInvariants = line.StartsWith("## Invariantes Globais", StringComparison.OrdinalIgnoreCase);
                    section = SpecSection.None;
                    if (!inGlobalInvariants)
                        currentScenario = null;
                }

                var scenarioMatch = scenarioHeader.Match(line);
                if (scenarioMatch.Success)
                {
                    currentScenario = new ScenarioDefinition
                    {
                        Id = scenarioMatch.Groups[1].Value,
                        Title = scenarioMatch.Groups[2].Value,
                        Hard = Array.Empty<(string, Regex)>(),
                        Soft = Array.Empty<(string, Regex)>(),
                        OrderRules = Array.Empty<(string, Regex, Regex)>()
                    };
                    spec.Scenarios.Add(currentScenario);
                    section = SpecSection.None;
                    inGlobalInvariants = false;
                    continue;
                }

                if (line.StartsWith("#### ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("### ", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.IndexOf("HARD", StringComparison.OrdinalIgnoreCase) >= 0)
                        section = SpecSection.Hard;
                    else if (line.IndexOf("SOFT", StringComparison.OrdinalIgnoreCase) >= 0)
                        section = SpecSection.Soft;
                    else if (line.IndexOf("Ordem", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("Order", StringComparison.OrdinalIgnoreCase) >= 0)
                        section = SpecSection.Order;
                    else
                        section = SpecSection.None;
                    continue;
                }

                var orderMatch = orderLine.Match(line);
                if (orderMatch.Success && section == SpecSection.Order)
                {
                    try
                    {
                        var key = orderMatch.Groups[1].Value;
                        var before = new Regex(orderMatch.Groups[2].Value, opt);
                        var after = new Regex(orderMatch.Groups[3].Value, opt);

                        if (inGlobalInvariants)
                        {
                            spec.GlobalOrderRules.Add((key, before, after));
                        }
                        else if (currentScenario != null)
                        {
                            var list = currentScenario.OrderRules.ToList();
                            list.Add((key, before, after));
                            currentScenario.OrderRules = list.ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        spec.Errors.Add($"Invalid order rule regex: {orderMatch.Groups[1].Value} ({ex.Message})");
                    }
                    continue;
                }

                var evidenceMatch = evidenceLine.Match(line);
                if (evidenceMatch.Success && (section == SpecSection.Hard || section == SpecSection.Soft))
                {
                    try
                    {
                        var key = evidenceMatch.Groups[1].Value;
                        var regex = new Regex(evidenceMatch.Groups[2].Value, opt);

                        if (inGlobalInvariants)
                        {
                            spec.GlobalHard.Add((key, regex));
                        }
                        else if (currentScenario != null)
                        {
                            if (section == SpecSection.Hard)
                            {
                                var list = currentScenario.Hard.ToList();
                                list.Add((key, regex));
                                currentScenario.Hard = list.ToArray();
                            }
                            else
                            {
                                var list = currentScenario.Soft.ToList();
                                list.Add((key, regex));
                                currentScenario.Soft = list.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        spec.Errors.Add($"Invalid evidence regex: {evidenceMatch.Groups[1].Value} ({ex.Message})");
                    }
                }
            }

            if (spec.Scenarios.Count == 0)
                spec.Errors.Add("Spec loaded without scenario definitions.");

            return spec;
        }

        private static InvariantResult EvaluateInvariants(string[] lines, SpecData spec)
        {
            var r = new InvariantResult
            {
                Result = "PASS"
            };

            foreach (var h in spec.GlobalHard)
            {
                if (!AnyMatch(lines, h.Pattern))
                    r.MissingHard.Add($"hard `{h.Key}`: `{h.Pattern}`");
            }

            foreach (var rule in spec.GlobalOrderRules)
            {
                int iBefore = FirstIndex(lines, rule.Before);
                int iAfter = FirstIndex(lines, rule.After);

                if (iBefore >= 0 && iAfter >= 0 && iBefore > iAfter)
                    r.OrderViolations.Add($"{rule.Key}: `{rule.Before}` appeared after `{rule.After}`");
            }

            if (r.MissingHard.Count > 0 || r.OrderViolations.Count > 0)
                r.Result = "FAIL";

            return r;
        }

        private static ScenarioResult EvaluateScenario(string[] lines, ScenarioDefinition def)
        {
            var r = new ScenarioResult
            {
                Id = def.Id,
                Title = def.Title,
                Result = "PASS"
            };

            foreach (var h in def.Hard)
            {
                if (!AnyMatch(lines, h.Pattern))
                    r.MissingHard.Add($"hard `{h.Key}`: `{h.Pattern}`");
            }

            foreach (var s in def.Soft)
            {
                if (!AnyMatch(lines, s.Pattern))
                    r.MissingSoft.Add($"soft `{s.Key}`: `{s.Pattern}`");
            }

            foreach (var rule in def.OrderRules)
            {
                int iBefore = FirstIndex(lines, rule.Before);
                int iAfter = FirstIndex(lines, rule.After);

                if (iBefore >= 0 && iAfter >= 0 && iBefore > iAfter)
                    r.OrderViolations.Add($"{rule.Key}: `{rule.Before}` appeared after `{rule.After}`");
            }

            if (r.MissingHard.Count > 0 || r.OrderViolations.Count > 0)
                r.Result = "FAIL";

            return r;
        }

        private static bool AnyMatch(string[] lines, Regex pattern)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                    return true;
            }
            return false;
        }

        private static int FirstIndex(string[] lines, Regex pattern)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                    return i;
            }
            return -1;
        }
    }
}
