using System;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
#if UNITY_EDITOR
    /// <summary>
    /// Runner determinístico para o smoke test PlayerMovementLeak em batchmode/CI.
    /// Garante que a cena correta é aberta antes de entrar em PlayMode e que o Editor encerra com o ExitCode apropriado.
    /// </summary>
    public static class PlayerMovementLeakSmokeBootstrapCI
    {
        private const string LogTag = "[PlayerMoveTest][Leak]";
        private const string TargetScenePath = "Assets/_ImmersiveGames/NewScripts/Scenes/NewBootstrap.unity";
        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/PlayerMovement-Leak.md";
        private const string MenuPath = "QA/Run PlayerMovement Leak Smoke (CI)";

        [MenuItem(MenuPath, priority = 2000)]
        public static void RunFromMenu()
        {
            Run();
        }

        /// <summary>
        /// Executa a preparação de cena e inicia PlayMode para o smoke test.
        /// </summary>
        public static void Run()
        {
            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrapCI),
                $"{LogTag} CI Runner iniciado (batchmode={Application.isBatchMode}, playing={EditorApplication.isPlaying}). Cena alvo='{TargetScenePath}'.");

            if (!AssetDatabase.AssetPathExists(TargetScenePath))
            {
                string reason = $"Cena alvo não encontrada ({TargetScenePath}).";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} INCONCLUSIVE - {reason}");

                WriteInconclusiveReport(reason);
                Environment.ExitCode = 3;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(Environment.ExitCode);
                }

                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.IsLoaded)
            {
                string reason = $"Falha ao abrir a cena alvo ({TargetScenePath}).";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} INCONCLUSIVE - {reason}");

                WriteInconclusiveReport(reason);
                Environment.ExitCode = 3;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(Environment.ExitCode);
                }

                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        }

        private static void WriteInconclusiveReport(string reason)
        {
            try
            {
                var dir = Path.GetDirectoryName(ReportPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var builder = new StringBuilder();
                builder.AppendLine("# Player Movement Leak Smoke Result");
                builder.AppendLine($"- Timestamp (UTC): {DateTime.UtcNow:O}");
                builder.AppendLine($"- Cena ativa: (não iniciada)");
                builder.AppendLine($"- Resultado final: INCONCLUSIVE");
                builder.AppendLine($"- Motivo: {reason}");
                builder.AppendLine();
                builder.AppendLine("## Métricas");
                builder.AppendLine("- Teste A (Gate fecha): status=N/A velInicial=0,000 velApósGate=0,000 driftApósGate=0,000 detalhe=");
                builder.AppendLine("- Teste B (Reset limpa estado): status=N/A velApósReset=0,000 driftApósReset=0,000 detalhe=");
                builder.AppendLine("- Teste C (Reabrir gate): status=N/A velApósReabertura=0,000 driftApósReabertura=0,000 detalhe=");
                builder.AppendLine();
                builder.AppendLine("## Logs (até 50 entradas)");
                builder.AppendLine("- Runner não executou o teste por ausência da cena alvo.");

                File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} Falha ao escrever relatório INCONCLUSIVE: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
#endif
}
