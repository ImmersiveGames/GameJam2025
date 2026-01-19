// Assets/_ImmersiveGames/NewScripts/QA/Baseline22/Baseline22QaInstaller.cs
// Installer do QA unificado (Baseline 2.2).
// Comentários PT; código EN.

#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline22
{
    public sealed class Baseline22QaInstaller : MonoBehaviour
    {
        private static Baseline22QaInstaller? _instance;
        private const string QaGameObjectName = "QA_Baseline22";

        public static void EnsureInstalled()
        {
            if (_instance != null)
            {
                return;
            }

            if (TryResolveExisting(out var existing))
            {
                _instance = existing;
                return;
            }

            var go = new GameObject(QaGameObjectName);
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Baseline22QaInstaller>();

            DebugUtility.Log<Baseline22QaInstaller>(
                "[QA][Baseline22] Baseline22QaContextMenu instalado (DontDestroyOnLoad).",
                DebugUtility.Colors.Ok);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureContextMenu();
        }

        private static bool TryResolveExisting(out Baseline22QaInstaller resolved)
        {
            resolved = FindExistingInstaller();
            if (resolved != null)
            {
                resolved.EnsureContextMenu();
                return true;
            }

            return false;
        }

        private static Baseline22QaInstaller FindExistingInstaller()
        {
            var installers = FindObjectsOfType<Baseline22QaInstaller>(true);
            if (installers == null || installers.Length == 0)
            {
                return null;
            }

            return installers[0];
        }

        private void EnsureContextMenu()
        {
            if (!gameObject.TryGetComponent<Baseline22QaContextMenu>(out _))
            {
                gameObject.AddComponent<Baseline22QaContextMenu>();
                DebugUtility.Log<Baseline22QaInstaller>(
                    "[QA][Baseline22] Baseline22QaContextMenu ausente; componente adicionado.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.Log<Baseline22QaInstaller>(
                "[QA][Baseline22] Para acessar o ContextMenu, selecione o GameObject 'QA_Baseline22' no Hierarchy (DontDestroyOnLoad).",
                DebugUtility.Colors.Ok);
        }
    }
}
