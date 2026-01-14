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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallOnLoad()
        {
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            if (TryResolveExisting(out var resolved))
            {
                EnsureContextMenu(resolved.gameObject);
                DebugUtility.Log<PregameQaInstaller>(
                    "[QA][Pregame] QA_Pregame já presente; instalação ignorada.",
                    DebugUtility.Colors.Info);
                LogInspectorHint();
                return;
            }

            try
            {
                var go = new GameObject(QaGameObjectName);
                _instance = go.AddComponent<PregameQaInstaller>();
                EnsureContextMenu(go);
                DontDestroyOnLoad(go);

                DebugUtility.Log<PregameQaInstaller>(
                    "[QA][Pregame] PregameQaContextMenu instalado (DontDestroyOnLoad).",
                    DebugUtility.Colors.Info);
                LogInspectorHint();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameQaInstaller>(
                    $"[QA][Pregame] Falha ao instalar PregameQaContextMenu. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool TryResolveExisting(out PregameQaInstaller resolved)
        {
            resolved = null;
            if (_instance != null)
            {
                resolved = _instance;
                return true;
            }

            var existing = FindExistingInstaller();
            if (existing == null)
            {
                return false;
            }

            _instance = existing;
            resolved = existing;
            return true;
        }

        private static PregameQaInstaller FindExistingInstaller()
        {
            var installers = FindObjectsOfType<PregameQaInstaller>(true);
            if (installers == null || installers.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < installers.Length; i++)
            {
                if (installers[i] == null)
                {
                    continue;
                }

                return installers[i];
            }

            return null;
        }

        private static void EnsureContextMenu(GameObject go)
        {
            if (go == null)
            {
                DebugUtility.LogWarning<PregameQaInstaller>(
                    "[QA][Pregame] QA_Pregame não disponível; ContextMenu não pode ser anexado.");
                return;
            }

            if (!go.TryGetComponent<PregameQaContextMenu>(out _))
            {
                go.AddComponent<PregameQaContextMenu>();
                DebugUtility.Log<PregameQaInstaller>(
                    "[QA][Pregame] PregameQaContextMenu ausente; componente adicionado.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void LogInspectorHint()
        {
            DebugUtility.Log<PregameQaInstaller>(
                "[QA][Pregame] Para acessar o ContextMenu, selecione o GameObject 'QA_Pregame' no Hierarchy (DontDestroyOnLoad).",
                DebugUtility.Colors.Info);
        }
    }
}
