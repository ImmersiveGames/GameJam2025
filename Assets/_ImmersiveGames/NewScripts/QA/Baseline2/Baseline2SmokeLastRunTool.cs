// Baseline2SmokeLastRunTool.cs
// Mantém captura e geração de relatório do Baseline 2.0 em um único arquivo.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Baseline2
{
    internal static class Baseline2SmokeLastRunShared
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string LastRunLogFile = "Baseline-2.0-Smoke-LastRun.log";
        internal const string LastRunMdFile = "Baseline-2.0-Smoke-LastRun.md";
        internal const string SpecFile = "Baseline-2.0-Spec.md";
        private const string StateFileName = "Baseline-2.0-Smoke-LastRun.state";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string LastRunLogAbs => Path.Combine(ReportsDirAbs, LastRunLogFile);
        internal static string LastRunMdAbs => Path.Combine(ReportsDirAbs, LastRunMdFile);
        internal static string SpecAbs => Path.Combine(ReportsDirAbs, SpecFile);

        internal static string StateFilePath
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                return Path.Combine(projectRoot, "Library", "Temp", StateFileName);
            }
        }

        internal struct StateData
        {
            public bool Armed;
            public bool Capturing;
            public string LogPath;
            public string CaptureId;
            public string CaptureStartUtc;
        }

        internal static StateData LoadState()
        {
            if (!File.Exists(StateFilePath))
                return new StateData { LogPath = LastRunLogAbs };

            var state = new StateData { LogPath = LastRunLogAbs };

            foreach (var line in File.ReadAllLines(StateFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                    continue;

                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "armed":
                        state.Armed = value == "1";
                        break;
                    case "capturing":
                        state.Capturing = value == "1";
                        break;
                    case "logPath":
                        state.LogPath = value;
                        break;
                    case "captureId":
                        state.CaptureId = value;
                        break;
                    case "captureStartUtc":
                        state.CaptureStartUtc = value;
                        break;
                }
            }

            if (string.IsNullOrEmpty(state.LogPath))
                state.LogPath = LastRunLogAbs;

            return state;
        }

        internal static void SaveState(StateData state)
        {
            var dir = Path.GetDirectoryName(StateFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                $"armed={(state.Armed ? 1 : 0)}",
                $"capturing={(state.Capturing ? 1 : 0)}",
                $"logPath={state.LogPath}",
                $"captureId={state.CaptureId}",
                $"captureStartUtc={state.CaptureStartUtc}"
            };

            File.WriteAllLines(StateFilePath, lines);
        }

        internal static StateData CreateArmedState(string logPath)
        {
            return new StateData
            {
                Armed = true,
                Capturing = false,
                LogPath = logPath,
                CaptureId = string.Empty,
                CaptureStartUtc = string.Empty
            };
        }

        internal static StateData CreateIdleState(string logPath)
        {
            return new StateData
            {
                Armed = false,
                Capturing = false,
                LogPath = logPath,
                CaptureId = string.Empty,
                CaptureStartUtc = string.Empty
            };
        }
    }

    internal static class Baseline2SmokeLastRunRuntime
    {
        private static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private static readonly object WriterLock = new object();
        private static StreamWriter _writer;
        private static bool _capturing;
        private static DateTime _captureStartUtc;
        private static string _captureId;
        private static string _logPath;
        private static Runner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeBoot()
        {
            var state = Baseline2SmokeLastRunShared.LoadState();
            if (!state.Armed && !state.Capturing)
                return;

            StartCapture(state, resume: state.Capturing);
        }

        internal static bool TryStartCaptureFromEditor()
        {
            if (!Application.isPlaying)
                return false;

            if (_capturing)
                return true;

            var state = Baseline2SmokeLastRunShared.LoadState();
            StartCapture(state, resume: state.Capturing);
            return true;
        }

        internal static bool StopCapture(string reason)
        {
            var state = Baseline2SmokeLastRunShared.LoadState();
            if (!_capturing && !state.Capturing)
                return false;

            Application.logMessageReceivedThreaded -= OnLogThreaded;

            var endUtc = DateTime.UtcNow;
            var duration = endUtc - _captureStartUtc;

            WriteLine("------------------------------------------------------------");
            WriteLine($"[Baseline2Smoke] CAPTURE STOPPED. utc={endUtc:O} duration={duration.TotalSeconds:F2}s reason={reason}");

            FlushQueueToDisk();
            SafeCloseWriter();

            _capturing = false;
            _captureId = string.Empty;
            _captureStartUtc = default;

            Baseline2SmokeLastRunShared.SaveState(Baseline2SmokeLastRunShared.CreateIdleState(state.LogPath));
            return true;
        }

        private static void StartCapture(Baseline2SmokeLastRunShared.StateData state, bool resume)
        {
            if (_capturing)
                return;

            _logPath = string.IsNullOrEmpty(state.LogPath)
                ? Baseline2SmokeLastRunShared.LastRunLogAbs
                : state.LogPath;

            var logDir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(logDir))
                Directory.CreateDirectory(logDir);

            bool append = resume && File.Exists(_logPath);

            _writer = new StreamWriter(
                _logPath,
                append,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            )
            { AutoFlush = false };

            _capturing = true;
            _captureStartUtc = DateTime.UtcNow;
            _captureId = string.IsNullOrEmpty(state.CaptureId) ? Guid.NewGuid().ToString("N") : state.CaptureId;

            Application.logMessageReceivedThreaded += OnLogThreaded;
            EnsureRunner();

            var updated = state;
            updated.Armed = false;
            updated.Capturing = true;
            updated.LogPath = _logPath;
            updated.CaptureId = _captureId;
            updated.CaptureStartUtc = _captureStartUtc.ToString("O");
            Baseline2SmokeLastRunShared.SaveState(updated);

            if (append)
                WriteLine($"[Baseline2Smoke] CAPTURE RESUMED. utc={_captureStartUtc:O} captureId={_captureId}");
            else
                WriteLine($"[Baseline2Smoke] CAPTURE STARTED. utc={_captureStartUtc:O} captureId={_captureId}");

            WriteLine($"[Baseline2Smoke] Output: {_logPath}");
            WriteLine("------------------------------------------------------------");

            Debug.Log($"[Baseline2Smoke] CAPTURE STARTED -> {_logPath}");
        }

        private static void EnsureRunner()
        {
            if (_runner != null)
                return;

            var go = new GameObject("~Baseline2SmokeLastRunRuntime");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        private static void OnLogThreaded(string condition, string stackTrace, LogType type)
        {
            string line;

            if (type == LogType.Exception || type == LogType.Error)
                line = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";
            else
                line = condition;

            line = line.Replace("\r\n", "\n").Replace("\r", "\n");

            foreach (var entry in line.Split('\n'))
            {
                if (!string.IsNullOrEmpty(entry))
                    Queue.Enqueue(entry);
            }
        }

        private static void FlushQueueToDisk()
        {
            if (_writer == null)
                return;

            lock (WriterLock)
            {
                while (Queue.TryDequeue(out var line))
                    _writer.WriteLine(line);

                _writer.Flush();
            }
        }

        private static void WriteLine(string line)
        {
            if (_writer == null)
                return;

            lock (WriterLock)
            {
                _writer.WriteLine(line);
                _writer.Flush();
            }
        }

        private static void SafeCloseWriter()
        {
            try
            {
                lock (WriterLock)
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
            }
            catch
            {
                // Ignorado por segurança.
            }
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                FlushQueueToDisk();
            }
        }
    }
}

#if UNITY_EDITOR
namespace _ImmersiveGames.NewScripts.EditorTools.Baseline2
{
    /// <summary>
    /// Ferramenta do editor para captura e relatório do Baseline 2.0 (última execução).
    /// - Um único item de menu (toggle Start/Stop).
    /// - Captura iniciada desde o startup via runtime (BeforeSceneLoad).
    /// - Stop salva log e gera relatório .md.
    /// </summary>
    [InitializeOnLoad]
    public static class Baseline2SmokeLastRunTool
    {
        private enum CaptureState
        {
            Idle,
            Armed,
            Capturing,
            ReportPending
        }

        private const string MenuPath = "Tools/NewScripts/Baseline2/Smoke Last Run (Start/Stop)";
        private const string PrefReportPending = "Baseline2.Smoke.ReportPending";

        static Baseline2SmokeLastRunTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;

            TryGenerateReportIfPending();
        }

        [MenuItem(MenuPath)]
        private static void ToggleEnabled()
        {
            var state = GetState();

            if (state == CaptureState.Capturing)
            {
                StopCaptureAndGenerateReport("EditorStop");
                Debug.Log("[Baseline2Smoke] Capture STOP solicitado. Log e relatório gerados.");
                return;
            }

            if (state == CaptureState.Armed)
            {
                Baseline2SmokeLastRunShared.SaveState(
                    Baseline2SmokeLastRunShared.CreateIdleState(Baseline2SmokeLastRunShared.LastRunLogAbs)
                );
                Debug.Log("[Baseline2Smoke] Capture DESARMADO.");
                return;
            }

            ArmCaptureAndEnterPlayMode();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            var state = GetState();
            Menu.SetChecked(MenuPath, state != CaptureState.Idle);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    Baseline2SmokeLastRunRuntime.TryStartCaptureFromEditor();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    if (GetState() == CaptureState.Capturing)
                        StopCaptureAndGenerateReport("ExitingPlayMode");
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    TryGenerateReportIfPending();
                    break;
            }
        }

        private static void OnEditorUpdate()
        {
            if (!EditorPrefs.GetBool(PrefReportPending, false))
                return;

            TryGenerateReportIfPending();
        }

        private static void ArmCaptureAndEnterPlayMode()
        {
            Baseline2SmokeLastRunShared.SaveState(
                Baseline2SmokeLastRunShared.CreateArmedState(Baseline2SmokeLastRunShared.LastRunLogAbs)
            );

            EditorPrefs.SetBool(PrefReportPending, false);

            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[Baseline2Smoke] Capture ARMADO. Entrando em Play Mode...");
                EditorApplication.isPlaying = true;
                return;
            }

            Debug.Log("[Baseline2Smoke] Capture ARMADO durante Play Mode. Iniciando captura agora.");
            Baseline2SmokeLastRunRuntime.TryStartCaptureFromEditor();
        }

        private static void StopCaptureAndGenerateReport(string reason)
        {
            Baseline2SmokeLastRunRuntime.StopCapture(reason);

            Baseline2SmokeLastRunShared.SaveState(
                Baseline2SmokeLastRunShared.CreateIdleState(Baseline2SmokeLastRunShared.LastRunLogAbs)
            );

            if (!TryGenerateReportNow())
                EditorPrefs.SetBool(PrefReportPending, true);
            else
                EditorPrefs.SetBool(PrefReportPending, false);
        }

        private static bool TryGenerateReportIfPending()
        {
            if (!EditorPrefs.GetBool(PrefReportPending, false))
                return false;

            var success = TryGenerateReportNow();
            if (success)
                EditorPrefs.SetBool(PrefReportPending, false);

            return success;
        }

        private static bool TryGenerateReportNow()
        {
            try
            {
                var logPath = Baseline2SmokeLastRunShared.LastRunLogAbs;

                if (!File.Exists(logPath))
                {
                    Debug.LogWarning($"[Baseline2Smoke] Relatório ignorado: log não encontrado -> {logPath}");
                    return true;
                }

                var lines = File.ReadAllLines(logPath);
                var report = GenerateMarkdownReport(lines, logPath);

                Directory.CreateDirectory(Baseline2SmokeLastRunShared.ReportsDirAbs);
                File.WriteAllText(Baseline2SmokeLastRunShared.LastRunMdAbs, report, new UTF8Encoding(false));

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    AssetDatabase.Refresh();

                Debug.Log($"[Baseline2Smoke] Relatório gerado -> {Baseline2SmokeLastRunShared.LastRunMdAbs}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Baseline2Smoke] Falha ao gerar relatório: {ex}");
                return false;
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

            var spec = LoadSpec(Baseline2SmokeLastRunShared.SpecAbs);
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
            sb.AppendLine($"- Spec: `{Baseline2SmokeLastRunShared.SpecAbs}`");
            sb.AppendLine($"- Result: **{overall}**");
            sb.AppendLine();

            sb.AppendLine("## Scenario Results");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Title | Result | Missing (Hard) | Order Violations |");
            sb.AppendLine("|---|---|---:|---:|---:|");

            foreach (var r in scenarioResults)
                sb.AppendLine($"| {r.Id} | {r.Title} | **{r.Result}** | {r.MissingHard.Count} | {r.OrderViolations.Count} |");

            sb.AppendLine();
            sb.AppendLine("## Missing Soft (Summary)");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Missing (Soft) |");
            sb.AppendLine("|---|---:|");

            foreach (var r in scenarioResults)
                sb.AppendLine($"| {r.Id} | {r.MissingSoft.Count} |");

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
                var violation = ValidateOrderRule(lines, rule.Before, rule.After, rule.Key);
                if (!string.IsNullOrEmpty(violation))
                    r.OrderViolations.Add(violation);
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
                var violation = ValidateOrderRule(lines, rule.Before, rule.After, rule.Key);
                if (!string.IsNullOrEmpty(violation))
                    r.OrderViolations.Add(violation);
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

        private static string ValidateOrderRule(string[] lines, Regex before, Regex after, string ruleKey)
        {
            int open = 0;
            bool sawBefore = false;
            bool sawAfter = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (before.IsMatch(line))
                {
                    open++;
                    sawBefore = true;
                }

                if (after.IsMatch(line))
                {
                    sawAfter = true;
                    if (open == 0)
                        return $"Order violation: {ruleKey} (completed without started). before=`{before}`, after=`{after}`";

                    open--;
                }
            }

            if (open > 0 || (sawBefore && !sawAfter))
                return $"Order violation: {ruleKey} (started without completed). before=`{before}`, after=`{after}`";

            return string.Empty;
        }

        private static CaptureState GetState()
        {
            if (EditorPrefs.GetBool(PrefReportPending, false))
                return CaptureState.ReportPending;

            var state = Baseline2SmokeLastRunShared.LoadState();
            if (state.Capturing)
                return CaptureState.Capturing;
            if (state.Armed)
                return CaptureState.Armed;
            return CaptureState.Idle;
        }
    }
}
#endif
