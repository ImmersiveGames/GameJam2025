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

namespace _ImmersiveGames.NewScripts.QA.Baseline21
{
    internal static class Baseline21SmokeLastRunState
    {
        internal struct StateData
        {
            public bool Armed;
            public bool Capturing;
            public string LogPath;
            public string TimestampedLogPath;
            public string CaptureId;
            public string CaptureStartUtc;
        }

        internal static StateData LoadState()
        {
            if (!File.Exists(Baseline21SmokeLastRunPaths.StateFilePath))
                return new StateData { LogPath = Baseline21SmokeLastRunPaths.LastRunLogAbs };

            var state = new StateData { LogPath = Baseline21SmokeLastRunPaths.LastRunLogAbs };

            foreach (var line in File.ReadAllLines(Baseline21SmokeLastRunPaths.StateFilePath))
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
                    case "timestampedLogPath":
                        state.TimestampedLogPath = value;
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
                state.LogPath = Baseline21SmokeLastRunPaths.LastRunLogAbs;

            return state;
        }

        internal static void SaveState(StateData state)
        {
            var dir = Path.GetDirectoryName(Baseline21SmokeLastRunPaths.StateFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                $"armed={(state.Armed ? 1 : 0)}",
                $"capturing={(state.Capturing ? 1 : 0)}",
                $"logPath={state.LogPath}",
                $"timestampedLogPath={state.TimestampedLogPath}",
                $"captureId={state.CaptureId}",
                $"captureStartUtc={state.CaptureStartUtc}"
            };

            File.WriteAllLines(Baseline21SmokeLastRunPaths.StateFilePath, lines);
        }

        internal static StateData CreateArmedState(string logPath, string timestampedLogPath)
        {
            return new StateData
            {
                Armed = true,
                Capturing = false,
                LogPath = logPath,
                TimestampedLogPath = timestampedLogPath,
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
                TimestampedLogPath = string.Empty,
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
        private static StreamWriter _timestampedWriter;
        private static bool _capturing;
        private static DateTime _captureStartUtc;
        private static string _captureId;
        private static string _logPath;
        private static string _timestampedLogPath;

        private static int _enqueuedSinceFlush;
        private const int AutoFlushThreshold = 256;

        internal static bool IsCapturing => _capturing;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void RuntimeBoot()
        {
            var state = Baseline21SmokeLastRunState.LoadState();
            if (!state.Armed && !state.Capturing)
                return;

            StartCapture(state, resume: state.Capturing);
        }

        internal static bool TryStartCaptureFromEditor(bool enableTimestamped)
        {
            if (!Application.isPlaying)
                return false;

            if (_capturing)
                return true;

            var state = Baseline21SmokeLastRunState.LoadState();
            if (!state.Armed && !state.Capturing)
                return false;

            if (enableTimestamped && string.IsNullOrEmpty(state.TimestampedLogPath))
            {
                state.TimestampedLogPath = Baseline21SmokeLastRunPaths.GetTimestampedLogAbs(DateTime.UtcNow);
                Baseline21SmokeLastRunState.SaveState(state);
            }

            StartCapture(state, resume: state.Capturing);
            return true;
        }

        internal static bool StopCapture(string reason)
        {
            var state = Baseline21SmokeLastRunState.LoadState();

            if (!_capturing && !state.Capturing)
                return false;

            Application.logMessageReceivedThreaded -= OnLogThreaded;

            var endUtc = DateTime.UtcNow;
            var duration = (_captureStartUtc == default) ? TimeSpan.Zero : (endUtc - _captureStartUtc);

            WriteLine("------------------------------------------------------------");
            WriteLine($"[Baseline21Smoke] CAPTURE STOPPED. utc={endUtc:O} duration={duration.TotalSeconds:F2}s reason={reason}");

            FlushQueueToDisk();
            SafeCloseWriters();

            _capturing = false;
            _captureId = string.Empty;
            _captureStartUtc = default;
            _logPath = string.Empty;
            _timestampedLogPath = string.Empty;
            Interlocked.Exchange(ref _enqueuedSinceFlush, 0);

            Baseline21SmokeLastRunState.SaveState(
                Baseline21SmokeLastRunState.CreateIdleState(string.IsNullOrEmpty(state.LogPath)
                    ? Baseline21SmokeLastRunPaths.LastRunLogAbs
                    : state.LogPath)
            );

            return true;
        }

        private static void StartCapture(Baseline21SmokeLastRunState.StateData state, bool resume)
        {
            if (_capturing)
                return;

            _logPath = string.IsNullOrEmpty(state.LogPath)
                ? Baseline21SmokeLastRunPaths.LastRunLogAbs
                : state.LogPath;

            _timestampedLogPath = state.TimestampedLogPath;

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

            if (!string.IsNullOrEmpty(_timestampedLogPath))
            {
                var tsDir = Path.GetDirectoryName(_timestampedLogPath);
                if (!string.IsNullOrEmpty(tsDir))
                    Directory.CreateDirectory(tsDir);

                _timestampedWriter = new StreamWriter(
                    _timestampedLogPath,
                    append: false,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                )
                { AutoFlush = false };
            }

            _capturing = true;

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
            Baseline21SmokeLastRunState.SaveState(updated);

            if (append)
                WriteLine($"[Baseline21Smoke] CAPTURE RESUMED. utc={DateTime.UtcNow:O} captureId={_captureId}");
            else
                WriteLine($"[Baseline21Smoke] CAPTURE STARTED. utc={DateTime.UtcNow:O} captureId={_captureId}");

            WriteLine($"[Baseline21Smoke] Output: {_logPath}");
            if (!string.IsNullOrEmpty(_timestampedLogPath))
                WriteLine($"[Baseline21Smoke] Output (timestamped): {_timestampedLogPath}");

            WriteLine("------------------------------------------------------------");

            Debug.Log($"[Baseline21Smoke] CAPTURE STARTED -> {_logPath}");
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
                {
                    _writer.WriteLine(line);
                    _timestampedWriter?.WriteLine(line);
                }

                _writer.Flush();
                _timestampedWriter?.Flush();
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

                if (_timestampedWriter != null)
                {
                    _timestampedWriter.WriteLine(line);
                    _timestampedWriter.Flush();
                }
            }
        }

        private static void SafeCloseWriters()
        {
            try
            {
                lock (WriterLock)
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;

                    _timestampedWriter?.Flush();
                    _timestampedWriter?.Dispose();
                    _timestampedWriter = null;
                }
            }
            catch
            {
                // Ignorado por segurança.
            }
        }
    }
}

#if UNITY_EDITOR
namespace _ImmersiveGames.NewScripts.EditorTools.Baseline21
{
    [InitializeOnLoad]
    public static class Baseline21SmokeLastRunTool
    {
        private const string MenuPathStart = "Tools/NewScripts/Baseline2.1/Smoke Last Run Start";
        private const string MenuPathStop = "Tools/NewScripts/Baseline2.1/Smoke Last Run Stop";
        private const string PrefTimestamped = "Baseline21.Smoke.WriteTimestamped";

        static Baseline21SmokeLastRunTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPathStart)]
        private static void StartCaptureMenu()
        {
            var enableTimestamped = EditorPrefs.GetBool(PrefTimestamped, false);
            var state = _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunState.LoadState();

            if (string.IsNullOrEmpty(state.TimestampedLogPath) && enableTimestamped)
                state.TimestampedLogPath = _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunPaths.GetTimestampedLogAbs(DateTime.UtcNow);

            _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunState.SaveState(
                _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunState.CreateArmedState(
                    _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunPaths.LastRunLogAbs,
                    state.TimestampedLogPath)
            );

            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[Baseline21Smoke] Capture ARMADO. Pressione Play para iniciar a captura.");
                return;
            }

            Debug.Log("[Baseline21Smoke] Capture ARMADO durante Play Mode. Iniciando captura agora.");
            _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor(enableTimestamped);
        }

        [MenuItem(MenuPathStop)]
        private static void StopCaptureMenu()
        {
            _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunRuntime.StopCapture("EditorStop");

            _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunState.SaveState(
                _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunState.CreateIdleState(
                    _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunPaths.LastRunLogAbs)
            );

            Debug.Log("[Baseline21Smoke] Capture STOP solicitado.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    var enableTimestamped = EditorPrefs.GetBool(PrefTimestamped, false);
                    _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunRuntime.TryStartCaptureFromEditor(enableTimestamped);
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                    _ImmersiveGames.NewScripts.QA.Baseline21.Baseline21SmokeLastRunRuntime.StopCapture("ExitingPlayMode");
                    break;
            }
        }
    }
}
#endif
