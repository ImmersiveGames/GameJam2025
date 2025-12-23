using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA.PlayModeTests
{
    /// <summary>
    /// PlayMode test automatizado para validar o Scene Flow nativo e bridge legado sem interação manual.
    /// Gera relatório markdown com evidências de execução (PASS/FAIL + logs marcados).
    /// </summary>
    public sealed class SceneFlowNativePlayModeTests
    {
        private const string ReportRelativePath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-PlayMode-Result.md";
#if NEWSCRIPTS_SCENEFLOW_NATIVE
        private const bool NativeEnabled = true;
        private const string NativeDefine = "NEWSCRIPTS_SCENEFLOW_NATIVE";
#else
        private const bool NativeEnabled = false;
        private const string NativeDefine = "NEWSCRIPTS_SCENEFLOW_NATIVE (ausente)";
#endif

        [UnityTest]
        public IEnumerator SceneFlowNative_ShouldExecuteSmokeAndReport()
        {
            var definedSymbols = GetDefinedSymbols();

            if (!NativeEnabled)
            {
                const string message = "NEWSCRIPTS_SCENEFLOW_NATIVE não definido; teste marcado como inconclusivo.";
                WriteReport("INCONCLUSIVE", definedSymbols, Array.Empty<string>(), new List<string> { message });
                Assert.Inconclusive(message);
            }

            var sceneFlowLogs = new List<string>();
            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (condition != null && condition.Contains("[SceneFlowTest]", StringComparison.Ordinal))
                {
                    sceneFlowLogs.Add(condition);
                }
            }

            Application.logMessageReceived += OnLog;

            var runnerObject = new GameObject("SceneFlowPlayModeRunner");
            var runner = runnerObject.AddComponent<NewScriptsInfraSmokeRunner>();
            var nativeTester = runnerObject.AddComponent<SceneTransitionServiceSmokeQATester>();
            var legacyTester = runnerObject.AddComponent<LegacySceneFlowBridgeSmokeQATester>();

            runner.ConfigureSceneFlowOnly(nativeTester, legacyTester, enableVerbose: true);

            // Espera 1 frame para inicializações de componentes.
            yield return null;

            Exception executionException = null;
            try
            {
                runner.RunAll();
            }
            catch (Exception ex)
            {
                executionException = ex;
                sceneFlowLogs.Add($"[SceneFlowTest][PlayMode] Exception capturada: {ex.GetType().Name}: {ex.Message}");
            }

            // Permite flush de logs e event loops do frame atual.
            yield return null;

            Application.logMessageReceived -= OnLog;

            bool failed = executionException != null || runner.LastRunHadFailures || ContainsFailureHint(sceneFlowLogs);
            string result = failed ? "FAIL" : "PASS";

            var testersRan = new[]
            {
                nameof(SceneTransitionServiceSmokeQATester),
                nameof(LegacySceneFlowBridgeSmokeQATester)
            };

            WriteReport(result, definedSymbols, testersRan, sceneFlowLogs);

            UnityEngine.Object.Destroy(runnerObject);

            if (failed)
            {
                Assert.Fail($"Scene Flow PlayMode test falhou; consulte o relatório em {ReportRelativePath}");
            }
        }

        private static bool ContainsFailureHint(IEnumerable<string> logs)
        {
            foreach (var entry in logs)
            {
                if (entry.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.IndexOf("SKIP", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void WriteReport(string result, string defines, IEnumerable<string> testers, IReadOnlyList<string> logs)
        {
            try
            {
                var targetDirectory = Path.GetDirectoryName(ReportRelativePath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var builder = new StringBuilder();
                builder.AppendLine("# Scene Flow PlayMode Result");
                builder.AppendLine($"- Timestamp: {DateTime.UtcNow:O}");
                builder.AppendLine($"- Defines: {defines}");
                builder.AppendLine($"- Testers: {string.Join(", ", testers)}");
                builder.AppendLine($"- Result: {result}");
                builder.AppendLine();
                builder.AppendLine("## Logs relevantes");

                if (logs == null || logs.Count == 0)
                {
                    builder.AppendLine("- (sem logs marcados por [SceneFlowTest])");
                }
                else
                {
                    int count = 0;
                    const int maxLogs = 30;
                    foreach (var entry in logs)
                    {
                        builder.AppendLine($"- {entry}");
                        count++;
                        if (count >= maxLogs)
                        {
                            builder.AppendLine("- (truncado)");
                            break;
                        }
                    }
                }

                File.WriteAllText(ReportRelativePath, builder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneFlowTest][Report] Falha ao escrever relatório: {ex}");
            }
        }

        private static string GetDefinedSymbols()
        {
            var symbols = new List<string> { NativeDefine };

#if UNITY_INCLUDE_TESTS
            symbols.Add("UNITY_INCLUDE_TESTS");
#endif

#if UNITY_EDITOR
            symbols.Add("UNITY_EDITOR");
#endif

            return string.Join(", ", symbols);
        }
    }
}
