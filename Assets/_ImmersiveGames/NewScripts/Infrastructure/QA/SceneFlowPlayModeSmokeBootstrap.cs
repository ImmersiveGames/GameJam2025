using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke runner em PlayMode/CI para validar Scene Flow (nativo) sem NUnit.
    /// </summary>
    public sealed class SceneFlowPlayModeSmokeBootstrap : MonoBehaviour
    {
        private const string LogTag = "[SceneFlowTest][Smoke]";
        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Smoke-Result.md";
        private const float TimeoutSeconds = 30f;

        [SerializeField] private bool enabledInThisScene = false;
        [SerializeField] private bool requireQaScenePrefix = true;
        [SerializeField] private string[] allowedSceneNames;

        private readonly List<string> _sceneFlowLogs = new();
        private int _capturedLogCount;
        private bool _nativePassMarkerFound;
        private bool _hasFailMarker;
        private int _runnerLastPasses;
        private int _runnerLastFails;

        private void Start()
        {
            if (!ShouldRunInThisScene())
            {
                return;
            }

            StartCoroutine(RunSmoke());
        }

        private bool ShouldRunInThisScene()
        {
#if !UNITY_EDITOR
            return false;
#else
            if (!enabledInThisScene)
            {
                return false;
            }

            string sceneName = SceneManager.GetActiveScene().name;

            if (allowedSceneNames != null && allowedSceneNames.Length > 0)
            {
                foreach (string allowed in allowedSceneNames)
                {
                    if (!string.IsNullOrWhiteSpace(allowed) &&
                        string.Equals(sceneName, allowed, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            if (requireQaScenePrefix &&
                !sceneName.StartsWith("QA_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
#endif
        }

        private IEnumerator RunSmoke()
        {
            Application.logMessageReceived += OnLogMessage;

            var runnerObject = new GameObject("SceneFlowSmokeRunner");
            var runner = runnerObject.AddComponent<NewScriptsInfraSmokeRunner>();
            var nativeTester = runnerObject.AddComponent<SceneTransitionServiceSmokeQaTester>();

            runner.ConfigureSceneFlowOnly(nativeTester, enableVerbose: true);

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

                if (_nativePassMarkerFound)
                {
                    break;
                }

                yield return null;
            }

            Application.logMessageReceived -= OnLogMessage;

            _runnerLastPasses = runner.LastRunPasses;
            _runnerLastFails = runner.LastRunFails;

            bool fail =
                executionException != null ||
                runner.LastRunHadFailures ||
                _hasFailMarker ||
                !_nativePassMarkerFound;

            string result = fail ? "FAIL" : "PASS";

            _sceneFlowLogs.Add(result == "PASS" ? $"{LogTag} PASS" : $"{LogTag} FAIL");

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

            // Captura apenas logs marcados com [SceneFlowTest] (escopo do smoke).
            if (!condition.Contains("[SceneFlowTest]", StringComparison.Ordinal))
            {
                return;
            }

            _capturedLogCount++;
            _sceneFlowLogs.Add(condition);

            // Verifica PASS do tester nativo.
            // Importante: não depende de "Fails=0" ou de strings similares.
            if (condition.IndexOf("[SceneFlowTest][Native] PASS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _nativePassMarkerFound = true;
            }

            // Detecta FAIL com padrões específicos, evitando falso-positivo em "Fails=0" / "Failures", etc.
            if (ContainsHardFailMarker(condition))
            {
                _hasFailMarker = true;
            }
        }

        /// <summary>
        /// FAIL marker deve ser inequívoco. Não pode ser substring de "Fails=0".
        /// </summary>
        private static bool ContainsHardFailMarker(string condition)
        {
            // Normaliza.
            var upper = condition.ToUpperInvariant();

            // Casos inequívocos no próprio smoke/runner.
            if (upper.Contains(" RESULT=FAIL", StringComparison.Ordinal) ||
                upper.Contains(" STATUS=FAIL", StringComparison.Ordinal))
            {
                return true;
            }

            if (upper.Contains("] FAIL ", StringComparison.Ordinal) ||
                upper.Contains("] FAIL-", StringComparison.Ordinal) ||
                upper.Contains("] FAIL:", StringComparison.Ordinal) ||
                upper.Contains("] FAIL.", StringComparison.Ordinal) ||
                upper.EndsWith("] FAIL", StringComparison.Ordinal))
            {
                return true;
            }

            // Padrão comum de exceção registrada no log marcado.
            if (upper.Contains(" EXCEPTION", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
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
