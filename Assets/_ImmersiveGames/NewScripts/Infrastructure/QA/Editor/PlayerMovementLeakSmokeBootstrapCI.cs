#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Runner determinístico para o smoke test PlayerMovementLeak em batchmode/CI.
    /// - Abre a cena-alvo (NewBootstrap) por PATH (primário) e fallback por nome (secundário).
    /// - Não usa fallback para cenas "limpas" (ex.: SceneTeste), pois isso gera INCONCLUSIVE e mascara setup incorreto.
    /// - Em Editor interativo: se já estiver em PlayMode, não interfere.
    /// </summary>
    public static class PlayerMovementLeakSmokeBootstrapCI
    {
        private const string LogTag = "[PlayerMoveTest][Leak]";
        private const string MenuPath = "QA/Run PlayerMovement Leak Smoke (CI)";

        // Cena canônica para este smoke. CI deve falhar se ela não existir.
        private const string CanonScenePath = "Assets/_ImmersiveGames/Scenes/NewBootstrap.unity";
        private const string CanonSceneName = "NewBootstrap";

        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/PlayerMovement-Leak.md";

        [MenuItem(MenuPath, priority = 2000)]
        public static void RunFromMenu() => Run();

        /// <summary>
        /// Entry point para -executeMethod em CI/batchmode.
        /// </summary>
        public static void Run()
        {
            DebugUtility.Log(
                typeof(PlayerMovementLeakSmokeBootstrapCI),
                $"{LogTag} CI Runner iniciado (batchmode={Application.isBatchMode}, playing={EditorApplication.isPlaying}). " +
                $"Target='{CanonSceneName}' Path='{CanonScenePath}'."
            );

            // Evita que o menu "CI" bagunce o Editor quando você já está testando manualmente.
            if (!Application.isBatchMode && EditorApplication.isPlaying)
            {
                DebugUtility.Log(
                    typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} Ignorado: já está em PlayMode (use este runner em batchmode/CI ou pare o PlayMode)."
                );
                return;
            }

            // (Opcional) evita perder alterações locais (no CI isso não aparece, mas no Editor ajuda).
            // Se você preferir forçar sem perguntar, remova este bloco.
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    FailInconclusive("Cancelado pelo usuário ao salvar cenas modificadas antes de abrir o smoke target.");
                    return;
                }
            }

            // 1) Tenta por PATH (preferido)
            string scenePath = TryPickCanonicalScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                // 2) Fallback: resolve por nome (caso você mova a cena sem atualizar path).
                scenePath = TryResolveByName(CanonSceneName);
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                FailInconclusive(
                    "Cena alvo não encontrada. " +
                    $"Path tentado='{CanonScenePath}', Nome tentado='{CanonSceneName}'."
                );
                return;
            }

            // Abre a cena
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    FailInconclusive($"Cena aberta mas inválida/não carregada. path='{scenePath}'.");
                    return;
                }

                DebugUtility.Log(
                    typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} Cena '{scene.name}' aberta com sucesso (path='{scenePath}'). Entrando em PlayMode."
                );
            }
            catch (Exception ex)
            {
                FailInconclusive($"Exception ao abrir cena: {ex.GetType().Name}: {ex.Message}. path='{scenePath}'.");
                return;
            }

            // Entra em PlayMode (o smoke bootstrap PlayMode cuida do resto)
            if (!EditorApplication.isPlaying)
            {
                // delayCall reduz risco de edge-case do Editor ao alternar imediatamente após OpenScene.
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = true;
                    }
                };
            }
        }

        private static string TryPickCanonicalScenePath()
        {
            // Para máxima compatibilidade entre versões, use LoadAssetAtPath em vez de AssetPathExists.
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CanonScenePath);
            return sceneAsset != null ? CanonScenePath : string.Empty;
        }

        private static string TryResolveByName(string name)
        {
            string filter = $"t:SceneAsset {name}";
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0)
            {
                return string.Empty;
            }

            var paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (paths.Length == 0)
            {
                return string.Empty;
            }

            // Preferir um match exato de nome do arquivo
            string exact = paths.FirstOrDefault(p =>
                p.EndsWith($"/{name}.unity", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith($"\\{name}.unity", StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrEmpty(exact) ? paths[0] : exact;
        }

        private static void FailInconclusive(string reason)
        {
            DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrapCI),
                $"{LogTag} INCONCLUSIVE - {reason}");

            WriteInconclusiveReport(reason);

            Environment.ExitCode = 3;

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(Environment.ExitCode);
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

                var sb = new StringBuilder();
                sb.AppendLine("# Player Movement Leak Smoke Result");
                sb.AppendLine($"- Timestamp (UTC): {DateTime.UtcNow:O}");
                sb.AppendLine("- Resultado final: INCONCLUSIVE");
                sb.AppendLine($"- Motivo: {reason}");
                sb.AppendLine();
                sb.AppendLine("## Logs");
                sb.AppendLine("- Runner não entrou em PlayMode (falha antes do start).");

                File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrapCI),
                    $"{LogTag} Falha ao escrever relatório INCONCLUSIVE: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
#endif
