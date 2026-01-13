#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Pregame
{
    public sealed class PregameQaInstaller : MonoBehaviour
    {
        private static PregameQaInstaller _instance;
        private const string QaGameObjectName = "QA_Pregame";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnLoad()
        {
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            if (TryResolveExisting())
            {
                return;
            }

            try
            {
                var go = new GameObject(QaGameObjectName);
                _instance = go.AddComponent<PregameQaInstaller>();
                go.AddComponent<PregameQaContextMenu>();
                DontDestroyOnLoad(go);

                DebugUtility.Log<PregameQaInstaller>(
                    "[QA][Pregame] PregameQaContextMenu instalado (DontDestroyOnLoad).",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameQaInstaller>(
                    $"[QA][Pregame] Falha ao instalar PregameQaContextMenu. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool TryResolveExisting()
        {
            if (_instance != null)
            {
                return true;
            }

            var existing = FindFirstObjectByType<PregameQaInstaller>(FindObjectsInactive.Include);
            if (existing == null)
            {
                return false;
            }

            _instance = existing;
            return true;
        }
    }
}
