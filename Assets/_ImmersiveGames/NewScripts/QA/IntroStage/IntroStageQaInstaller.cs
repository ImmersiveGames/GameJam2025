#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.IntroStage
{
    public sealed class IntroStageQaInstaller : MonoBehaviour
    {
        private static IntroStageQaInstaller _instance;
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
                DebugUtility.Log<IntroStageQaInstaller>(
                    "[QA][IntroStage] QA_IntroStage já presente; instalação ignorada.",
                    DebugUtility.Colors.Info);
                return;
            }

            try
            {
                var go = new GameObject(QaGameObjectName);
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<IntroStageQaInstaller>();

                DebugUtility.Log<IntroStageQaInstaller>(
                    "[QA][IntroStage] IntroStageQaContextMenu instalado (DontDestroyOnLoad).",
                    DebugUtility.Colors.Info);

                EnsureContextMenu(go);
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogWarning<IntroStageQaInstaller>(
                    $"[QA][IntroStage] Falha ao instalar IntroStageQaContextMenu. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool TryResolveExisting(out IntroStageQaInstaller resolved)
        {
            resolved = FindExistingInstaller();
            return resolved != null;
        }

        private static IntroStageQaInstaller FindExistingInstaller()
        {
            var installers = FindObjectsOfType<IntroStageQaInstaller>(true);
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
                DebugUtility.LogWarning<IntroStageQaInstaller>(
                    "[QA][IntroStage] QA_IntroStage não disponível; ContextMenu não pode ser anexado.");
                return;
            }

            if (!go.TryGetComponent<IntroStageQaContextMenu>(out _))
            {
                go.AddComponent<IntroStageQaContextMenu>();
                DebugUtility.Log<IntroStageQaInstaller>(
                    "[QA][IntroStage] IntroStageQaContextMenu ausente; componente adicionado.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.Log<IntroStageQaInstaller>(
                "[QA][IntroStage] Para acessar o ContextMenu, selecione o GameObject 'QA_IntroStage' no Hierarchy (DontDestroyOnLoad).",
                DebugUtility.Colors.Info);
        }
    }
}
