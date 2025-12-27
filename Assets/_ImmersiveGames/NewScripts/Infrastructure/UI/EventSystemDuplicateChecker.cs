using System.Linq;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.UI
{
    /// <summary>
    /// Diagnostica múltiplos EventSystem ativos na cena.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EventSystemDuplicateChecker : MonoBehaviour
    {
        private bool _checked;

        private void Awake()
        {
            CheckEventSystems("Awake");
        }

        private void Start()
        {
            CheckEventSystems("Start");
        }

        private void CheckEventSystems(string phase)
        {
            if (_checked)
            {
                return;
            }

            _checked = true;

            var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(system => system != null && system.isActiveAndEnabled)
                .ToArray();

            if (systems.Length > 1)
            {
                DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                    $"[UI] {systems.Length} EventSystems ativos detectados ({phase}).");

                foreach (var system in systems)
                {
                    var sceneName = system.gameObject.scene.name;
                    DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                        $"[UI] EventSystem ativo: name='{system.name}', scene='{sceneName}'.");
                }

                return;
            }

            if (systems.Length == 1)
            {
                var system = systems[0];
                var sceneName = system.gameObject.scene.name;
                DebugUtility.LogVerbose<EventSystemDuplicateChecker>(
                    $"[UI] EventSystem OK ({phase}). name='{system.name}', scene='{sceneName}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                $"[UI] Nenhum EventSystem ativo detectado ({phase}).");
        }
    }
}
