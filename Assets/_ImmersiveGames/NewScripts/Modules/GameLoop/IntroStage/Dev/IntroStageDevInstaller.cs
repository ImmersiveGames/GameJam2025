#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev
{
    public sealed class IntroStageDevInstaller : MonoBehaviour
    {
        private static IntroStageDevInstaller? _instance;
        private const string QaGameObjectName = "QA_IntroStage";

        public static void EnsureInstalled()
        {
            if (_instance != null)
            {
                return;
            }

            if (TryResolveExisting(out var existing))
            {
                _instance = existing;
                DebugUtility.Log<IntroStageDevInstaller>(
                    "[QA][IntroStageController] QA_IntroStage já presente; instalação ignorada.",
                    DebugUtility.Colors.Info);
                return;
            }

            try
            {
                var go = new GameObject(QaGameObjectName);
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<IntroStageDevInstaller>();

                DebugUtility.Log<IntroStageDevInstaller>(
                    "[QA][IntroStageController] IntroStageDevContextMenu instalado (DontDestroyOnLoad).",
                    DebugUtility.Colors.Info);

                EnsureContextMenu(go);
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogWarning<IntroStageDevInstaller>(
                    $"[QA][IntroStageController] Falha ao instalar IntroStageDevContextMenu. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool TryResolveExisting(out IntroStageDevInstaller? resolved)
        {
            resolved = FindExistingInstaller();
            return resolved != null;
        }

        private static IntroStageDevInstaller? FindExistingInstaller()
        {
            IntroStageDevInstaller[]? installers = FindObjectsByType<IntroStageDevInstaller>(FindObjectsSortMode.None);
            if (installers == null || installers.Length == 0)
            {
                return null;
            }

            return installers[0];
        }

        private static void EnsureContextMenu(GameObject go)
        {
            if (go == null)
            {
                DebugUtility.LogWarning<IntroStageDevInstaller>(
                    "[QA][IntroStageController] QA_IntroStage não disponível; ContextMenu não pode ser anexado.");
                return;
            }

            if (!go.TryGetComponent<IntroStageDevContextMenu>(out _))
            {
                go.AddComponent<IntroStageDevContextMenu>();
                DebugUtility.Log<IntroStageDevInstaller>(
                    "[QA][IntroStageController] IntroStageDevContextMenu ausente; componente adicionado.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.Log<IntroStageDevInstaller>(
                "[QA][IntroStageController] Para acessar o ContextMenu, selecione o GameObject 'QA_IntroStage' no Hierarchy (DontDestroyOnLoad).",
                DebugUtility.Colors.Info);
        }
    }
}
