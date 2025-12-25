using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow.QA;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke runner em PlayMode/CI para validar Scene Flow (nativo + bridge legado) sem NUnit.
    /// </summary>
    public sealed class SceneFlowPlayModeSmokeBootstrap : MonoBehaviour
    {
        private const string LogTag = "[SceneFlowTest][Smoke]";
        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Smoke-Result.md";
        private const float TimeoutSeconds = 30f;

#if NEWSCRIPTS_SCENEFLOW_NATIVE
        private const bool NativeEnabled = true;
#else
        private const bool NativeEnabled = false;
#endif

        private readonly List<string> _sceneFlowLogs = new();
        private int _capturedLogCount;
        private bool _nativePassMarkerFound;
        private bool _bridgePassMarkerFound;
        private bool _hasFailMarker;
        private int _runnerLastPasses;
        private int _runnerLastFails;

        private void Start()
        {
            StartCoroutine(RunSmoke());
        }

        private IEnumerator RunSmoke()
        {
            Application.logMessageReceived += OnLogMessage;

            var runnerObject = new GameObject("SceneFlowSmokeRunner");
            var runner = runnerObject.AddComponent<NewScriptsInfraSmokeRunner>();
            var nativeTester = runnerObject.AddComponent<SceneTransitionServiceSmokeQaTester>();
            var legacyTester = runnerObject.AddComponent<LegacySceneFlowBridgeSmokeQaTester>();

            runner.ConfigureSceneFlowOnly(nativeTester, legacyTester, enableVerbose: true);

            Exception executionException = null;
            try
            {
                runner.RunAll();
            }
            catch (Exception ex)
            {
                executionException = ex;
                _hasFailMarker = true;
                _sceneFlowLogs.Add($"{LogTag} Exception: {ex.GetType().Name}: {ex.Message}");
            }

            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < TimeoutSeconds)
            {
                if (_hasFailMarker || runner.LastRunHadFailures)
                {
                    break;
                }

                if (_nativePassMarkerFound && _bridgePassMarkerFound)
                {
                    break;
                }

                yield return null;
            }

            Application.logMessageReceived -= OnLogMessage;

            _runnerLastPasses = runner.LastRunPasses;
            _runnerLastFails = runner.LastRunFails;

            bool fail = executionException != null
                        || runner.LastRunHadFailures
                        || _hasFailMarker
                        || !_nativePassMarkerFound
                        || !_bridgePassMarkerFound;
            string result = fail ? "FAIL" : "PASS";

            if (result == "PASS")
            {
                _sceneFlowLogs.Add($"{LogTag} PASS");
            }
            else
            {
                _sceneFlowLogs.Add($"{LogTag} FAIL");
            }

            WriteReport(result);

            int exitCode = result == "PASS" ? 0 : 2;
            SetExit(exitCode, result);

            Destroy(runnerObject);

            if (Application.isBatchMode)
            {
                Application.Quit(Environment.ExitCode);
            }
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return;
            }

            if (condition.Contains("[SceneFlowTest]", StringComparison.Ordinal))
            {
                _capturedLogCount++;
                _sceneFlowLogs.Add(condition);

                if (condition.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    condition.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _hasFailMarker = true;
                }

                if (condition.IndexOf("[SceneFlowTest][Native] PASS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _nativePassMarkerFound = true;
                }

                if (condition.IndexOf("[SceneFlowTest][Bridge] PASS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _bridgePassMarkerFound = true;
                }
            }
        }

        private void WriteReport(string result)
        {
            try
            {
                var dir = Path.GetDirectoryName(ReportPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var builder = new StringBuilder();
                builder.AppendLine("# Scene Flow Smoke Result");
                builder.AppendLine($"- Timestamp: {DateTime.UtcNow:O}");
                builder.AppendLine($"- Defines: {GetDefines()}");
                builder.AppendLine($"- Result: {result}");
                builder.AppendLine($"- RunnerPasses: {_runnerLastPasses}");
                builder.AppendLine($"- RunnerFails: {_runnerLastFails}");
                builder.AppendLine($"- NativePassMarkerFound: {_nativePassMarkerFound}");
                builder.AppendLine($"- BridgePassMarkerFound: {_bridgePassMarkerFound}");
                builder.AppendLine($"- TotalMarkedLogsCaptured: {_capturedLogCount}");
                builder.AppendLine();
                builder.AppendLine("## Logs (até 30 entradas)");

                if (_sceneFlowLogs.Count == 0)
                {
                    builder.AppendLine("- (sem logs marcados por [SceneFlowTest])");
                }
                else
                {
                    int count = 0;
                    foreach (var entry in _sceneFlowLogs)
                    {
                        builder.AppendLine($"- {entry}");
                        count++;
                        if (count >= 30)
                        {
                            builder.AppendLine($"- (truncado; total={_sceneFlowLogs.Count})");
                            break;
                        }
                    }
                }

                File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneFlowPlayModeSmokeBootstrap),
                    $"{LogTag} Falha ao escrever relatório: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string GetDefines()
        {
            var defines = new List<string>();
#if NEWSCRIPTS_SCENEFLOW_NATIVE
            defines.Add("NEWSCRIPTS_SCENEFLOW_NATIVE");
#endif
#if UNITY_EDITOR
            defines.Add("UNITY_EDITOR");
#endif
#if UNITY_INCLUDE_TESTS
            defines.Add("UNITY_INCLUDE_TESTS");
#endif
            return string.Join(", ", defines);
        }

        private static void SetExit(int code, string result)
        {
            Environment.ExitCode = code;
            DebugUtility.Log(typeof(SceneFlowPlayModeSmokeBootstrap), $"{LogTag} RESULT={result} ExitCode={code}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Entry point para batchmode via -executeMethod.
    /// </summary>
    public static class SceneFlowPlayModeSmokeBootstrapCi
    {
        public static void Run()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        }
    }
#endif
}
