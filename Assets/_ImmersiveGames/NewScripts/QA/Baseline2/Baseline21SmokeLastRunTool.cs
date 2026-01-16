// Baseline21SmokeLastRunTool.cs
// Captura e gera relatório do Baseline 2.1 em arquivo dedicado (isolado do 2.0).

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Baseline2
{
    internal static class Baseline21SmokeLastRunShared
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string LastRunLogFile = "Baseline-2.1-Smoke-LastRun.log";
        internal const string LastRunMdFile = "Baseline-2.1-Smoke-LastRun.md";
        internal const string ObservabilityContractFile = "Observability-Contract.md";
        private const string StateFileName = "Baseline-2.1-Smoke-LastRun.state";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string LastRunLogAbs => Path.Combine(ReportsDirAbs, LastRunLogFile);
        internal static string LastRunMdAbs => Path.Combine(ReportsDirAbs, LastRunMdFile);
        internal static string ObservabilityContractAbs => Path.Combine(ReportsDirAbs, ObservabilityContractFile);

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
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
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

    internal static class Baseline21SmokeLastRunRuntime
    {
        private static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private static readonly object WriterLock = new object();

        private static StreamWriter _writer;
        private static bool _capturing;
        private static DateTime _captureStartUtc;
        private static string _captureId;
        private static string _logPath;

        private static int _enqueuedSinceFlush;
        private const int AutoFlushThreshold = 256;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void RuntimeBoot()
        {
            var state = Baseline21SmokeLastRunShared.LoadState();
            if (!state.Armed && !state.Capturing)
                return;

            StartCapture(state, resume: state.Capturing);
        }

        internal static bool IsCapturing => _capturing;

        internal static bool TryStartCaptureFromEditor()
        {
            if (!Application.isPlaying)
                return false;

            if (_capturing)
                return true;

            var state = Baseline21SmokeLastRunShared.LoadState();
            if (!state.Armed && !state.Capturing)
                return false;

            StartCapture(state, resume: state.Capturing);
            return true;
        }

        internal static bool StopCapture(string reason)
        {
            var state = Baseline21SmokeLastRunShared.LoadState();

            // Idempotente: se em memória não está capturando mas o state dizia capturando, ainda tentamos “fechar” com segurança.
            if (!_capturing && !state.Capturing)
                return false;

            Application.logMessageReceivedThreaded -= OnLogThreaded;

            var endUtc = DateTime.UtcNow;
            var duration = (_captureStartUtc == default) ? TimeSpan.Zero : (endUtc - _captureStartUtc);

            WriteLine("------------------------------------------------------------");
            WriteLine($"[Baseline21Smoke] CAPTURE STOPPED. utc={endUtc:O} duration={duration.TotalSeconds:F2}s reason={reason}");

            FlushQueueToDisk();
            SafeCloseWriter();
            TryWriteMarkdownReport(endUtc, duration);

            _capturing = false;
            _captureId = string.Empty;
            _captureStartUtc = default;
            _logPath = string.Empty;
            Interlocked.Exchange(ref _enqueuedSinceFlush, 0);

            Baseline21SmokeLastRunShared.SaveState(
                Baseline21SmokeLastRunShared.CreateIdleState(string.IsNullOrEmpty(state.LogPath) ? Baseline21SmokeLastRunShared.LastRunLogAbs : state.LogPath)
            );

            return true;
        }

        private static void StartCapture(Baseline21SmokeLastRunShared.StateData state, bool resume)
        {
            if (_capturing)
                return;

            _logPath = string.IsNullOrEmpty(state.LogPath)
                ? Baseline21SmokeLastRunShared.LastRunLogAbs
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

            // Se está “resumindo” e o state tinha startUtc, tentamos respeitar (senão, marca agora).
            if (resume && DateTime.TryParse(state.CaptureStartUtc, out var parsed))
                _captureStartUtc = parsed.ToUniversalTime();
            else
                _captureStartUtc = DateTime.UtcNow;

            _captureId = string.IsNullOrEmpty(state.CaptureId) ? Guid.NewGuid().ToString("N") : state.CaptureId;

            Application.logMessageReceivedThreaded += OnLogThreaded;

            var updated = state;
            updated.Armed = false;
            updated.Capturing = true;
            updated.LogPath = _logPath;
            updated.CaptureId = _captureId;
            updated.CaptureStartUtc = _captureStartUtc.ToString("O");
            Baseline21SmokeLastRunShared.SaveState(updated);

            if (!append)
                WriteHeader(_captureStartUtc, _captureId, _logPath);

            Debug.Log($"[Baseline21Smoke] CAPTURE STARTED -> {_logPath}");
        }

        private static void WriteHeader(DateTime startUtc, string captureId, string logPath)
        {
            WriteLine("============================================================");
            WriteLine("Baseline 2.1 — Smoke Last Run Log");
            WriteLine("Status: STARTED");
            WriteLine($"StartedUtc: {startUtc:O}");
            WriteLine($"CaptureId: {captureId}");
            WriteLine($"Output: {logPath}");
            WriteLine("============================================================");
        }

        private static void OnLogThreaded(string condition, string stackTrace, LogType type)
        {
            if (!_capturing)
                return;

            string line;

            if (type == LogType.Exception || type == LogType.Error)
                line = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";
            else
                line = condition;

            if (string.IsNullOrEmpty(line))
                return;

            line = line.Replace("\r\n", "\n").Replace("\r", "\n");

            foreach (var entry in line.Split('\n'))
            {
                if (!string.IsNullOrEmpty(entry))
                    Queue.Enqueue(entry);
            }

            // Auto-flush para evitar ficar “vazio” em disco em execuções curtas.
            var n = Interlocked.Add(ref _enqueuedSinceFlush, 1);
            if (n >= AutoFlushThreshold)
            {
                Interlocked.Exchange(ref _enqueuedSinceFlush, 0);
                FlushQueueToDisk();
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

        private static bool TryWriteMarkdownReport(DateTime endUtc, TimeSpan duration)
        {
            try
            {
                var logPath = Baseline21SmokeLastRunShared.LastRunLogAbs;
                if (!File.Exists(logPath))
                    return false;

                int lineCount;
                try
                {
                    lineCount = File.ReadAllLines(logPath).Length;
                }
                catch
                {
                    lineCount = 0;
                }

                var report = BuildMarkdownReport(logPath, lineCount, _captureStartUtc, endUtc, duration, _captureId);
                Directory.CreateDirectory(Baseline21SmokeLastRunShared.ReportsDirAbs);
                File.WriteAllText(Baseline21SmokeLastRunShared.LastRunMdAbs, report, new UTF8Encoding(false));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildMarkdownReport(
            string logPath,
            int lineCount,
            DateTime startUtc,
            DateTime endUtc,
            TimeSpan duration,
            string captureId)
        {
            var sb = new StringBuilder(8 * 1024);

            sb.AppendLine("# Baseline 2.1 — Smoke Last Run");
            sb.AppendLine();
            sb.AppendLine($"- utcStart: `{startUtc:O}`");
            sb.AppendLine($"- utcEnd: `{endUtc:O}`");
            sb.AppendLine($"- durationSeconds: `{duration.TotalSeconds:F2}`");
            sb.AppendLine($"- captureId: `{captureId}`");
            sb.AppendLine();
            sb.AppendLine("- Fonte de verdade: [Observability-Contract.md](./Observability-Contract.md)");
            sb.AppendLine("- Nota: o log é evidência.");
            sb.AppendLine();
            sb.AppendLine("## Log");
            sb.AppendLine();
            sb.AppendLine($"- Path: `{logPath}`");
            sb.AppendLine($"- Lines: `{lineCount}`");
            sb.AppendLine();
            sb.AppendLine("## Contrato");
            sb.AppendLine();
            sb.AppendLine($"- Contract path: `{Baseline21SmokeLastRunShared.ObservabilityContractAbs}`");

            return sb.ToString();
        }
    }
}

#if UNITY_EDITOR
namespace _ImmersiveGames.NewScripts.EditorTools.Baseline2
{
    /// <summary>
    /// Ferramenta do editor para captura e relatório do Baseline 2.1 (última execução).
    /// - Menu Start/Stop dedicado (isolado do 2.0).
    /// - Arm em Edit Mode: usuário dá Play manualmente (captura começa no startup do Play).
    /// - Stop em Play Mode: salva log e gera relatório .md.
    /// - Ao sair do Play Mode capturando, faz Stop automaticamente.
    /// </summary>
    [InitializeOnLoad]
    public static class Baseline21SmokeLastRunTool
    {
        private enum CaptureState
        {
            Idle,
            Armed,
            Capturing,
            ReportPending
        }

        private const string MenuPath = "Tools/NewScripts/Baseline2/Smoke 2.1 (Start/Stop)";
        private const string PrefReportPending = "Baseline21.Smoke.ReportPending";

        static Baseline21SmokeLastRunTool()
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
                Debug.Log("[Baseline21Smoke] Capture STOP solicitado. Log e relatório gerados.");
                return;
            }

            if (state == CaptureState.Armed)
            {
                Baseline21SmokeLastRunShared.SaveState(
                    Baseline21SmokeLastRunShared.CreateIdleState(Baseline21SmokeLastRunShared.LastRunLogAbs)
                );
                Debug.Log("[Baseline21Smoke] Capture DESARMADO.");
                return;
            }

            // Idle -> Arm (não força Play automaticamente; evita “reiniciar sem querer” após Stop).
            ArmCapture();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            var state = GetState();
            Menu.SetChecked(MenuPath, state != CaptureState.Idle);
            return true;
        }

        private static void ArmCapture()
        {
            Baseline21SmokeLastRunShared.SaveState(
                Baseline21SmokeLastRunShared.CreateArmedState(Baseline21SmokeLastRunShared.LastRunLogAbs)
            );

            EditorPrefs.SetBool(PrefReportPending, false);

            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[Baseline21Smoke] Capture ARMADO. Agora pressione Play para iniciar a captura desde o startup.");
                return;
            }

            Debug.Log("[Baseline21Smoke] Capture ARMADO durante Play Mode. Iniciando captura agora.");
            _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    // Se estava armado, o RuntimeBoot já terá iniciado cedo. Aqui é só um fallback.
                    _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor();
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

        private static void StopCaptureAndGenerateReport(string reason)
        {
            _ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.StopCapture(reason);

            Baseline21SmokeLastRunShared.SaveState(
                Baseline21SmokeLastRunShared.CreateIdleState(Baseline21SmokeLastRunShared.LastRunLogAbs)
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
                var logPath = Baseline21SmokeLastRunShared.LastRunLogAbs;
                var reportPath = Baseline21SmokeLastRunShared.LastRunMdAbs;

                if (File.Exists(reportPath))
                    return true;

                if (!File.Exists(logPath))
                {
                    Debug.LogWarning($"[Baseline21Smoke] Relatório ignorado: log não encontrado -> {logPath}");
                    return true;
                }

                var lines = File.ReadAllLines(logPath);
                var report = GenerateMarkdownReport(lines, logPath);

                Directory.CreateDirectory(Baseline21SmokeLastRunShared.ReportsDirAbs);
                File.WriteAllText(reportPath, report, new UTF8Encoding(false));

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    AssetDatabase.Refresh();

                Debug.Log($"[Baseline21Smoke] Relatório gerado -> {Baseline21SmokeLastRunShared.LastRunMdAbs}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Baseline21Smoke] Falha ao gerar relatório: {ex}");
                return false;
            }
        }

        private static string GenerateMarkdownReport(string[] lines, string sourcePath)
        {
            var generatedAtUtc = DateTime.UtcNow;
            var lineCount = lines?.Length ?? 0;
            var endUtc = generatedAtUtc;
            var startUtc = DateTime.MinValue;
            var captureId = string.Empty;

            TryExtractHeaderInfo(lines, out startUtc, out captureId);
            var duration = startUtc == DateTime.MinValue ? TimeSpan.Zero : endUtc - startUtc;

            var sb = new StringBuilder(8 * 1024);

            sb.AppendLine("# Baseline 2.1 — Smoke Last Run");
            sb.AppendLine();
            sb.AppendLine($"- utcStart: `{(startUtc == DateTime.MinValue ? \"n/a\" : startUtc.ToString(\"O\"))}`");
            sb.AppendLine($"- utcEnd: `{endUtc:O}`");
            sb.AppendLine($"- durationSeconds: `{duration.TotalSeconds:F2}`");
            sb.AppendLine($"- captureId: `{(string.IsNullOrEmpty(captureId) ? \"n/a\" : captureId)}`");
            sb.AppendLine();
            sb.AppendLine("- Fonte de verdade: [Observability-Contract.md](./Observability-Contract.md)");
            sb.AppendLine("- Nota: o log é evidência.");
            sb.AppendLine();
            sb.AppendLine("## Log");
            sb.AppendLine();
            sb.AppendLine($"- Path: `{sourcePath}`");
            sb.AppendLine($"- Lines: `{lineCount}`");
            sb.AppendLine();
            sb.AppendLine("## Contrato");
            sb.AppendLine();
            sb.AppendLine($"- Contract path: `{Baseline21SmokeLastRunShared.ObservabilityContractAbs}`");
            return sb.ToString();
        }

        private static void TryExtractHeaderInfo(string[] lines, out DateTime startUtc, out string captureId)
        {
            startUtc = DateTime.MinValue;
            captureId = string.Empty;

            if (lines == null || lines.Length == 0)
                return;

            foreach (var line in lines)
            {
                if (line.StartsWith("StartedUtc:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Substring("StartedUtc:".Length).Trim();
                    if (DateTime.TryParse(value, out var parsed))
                        startUtc = parsed.ToUniversalTime();
                }

                if (line.StartsWith("CaptureId:", StringComparison.OrdinalIgnoreCase))
                    captureId = line.Substring("CaptureId:".Length).Trim();

                if (startUtc != DateTime.MinValue && !string.IsNullOrEmpty(captureId))
                    break;
            }
        }

        private static CaptureState GetState()
        {
            if (EditorPrefs.GetBool(PrefReportPending, false))
                return CaptureState.ReportPending;

            if (_ImmersiveGames.NewScripts.QA.Baseline2.Baseline21SmokeLastRunRuntime.IsCapturing)
                return CaptureState.Capturing;

            var state = Baseline21SmokeLastRunShared.LoadState();
            if (state.Capturing)
                return CaptureState.Capturing;
            if (state.Armed)
                return CaptureState.Armed;
            return CaptureState.Idle;
        }
    }
}
#endif
