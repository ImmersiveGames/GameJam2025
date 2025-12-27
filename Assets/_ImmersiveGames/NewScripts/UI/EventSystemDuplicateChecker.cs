using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Diagnóstico simples para detectar múltiplos EventSystem ativos na cena.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EventSystemDuplicateChecker : MonoBehaviour
    {
        private void Start()
        {
            var allSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var activeSystems = new List<EventSystem>(allSystems.Length);

            foreach (var system in allSystems)
            {
                if (system == null)
                {
                    continue;
                }

                if (system.isActiveAndEnabled && system.gameObject.activeInHierarchy)
                {
                    activeSystems.Add(system);
                }
            }

            if (activeSystems.Count > 1)
            {
                DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                    $"[UI] Encontrados {activeSystems.Count} EventSystems ativos. Diagnóstico abaixo:");

                foreach (var system in activeSystems)
                {
                    var sceneName = system.gameObject.scene.IsValid()
                        ? system.gameObject.scene.name
                        : "<Scene inválida>";

                    DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                        $"[UI] EventSystem ativo: name='{system.gameObject.name}', scene='{sceneName}'.",
                        system.gameObject);
                }

                return;
            }

            if (activeSystems.Count == 1)
            {
                var system = activeSystems[0];
                var sceneName = system.gameObject.scene.IsValid()
                    ? system.gameObject.scene.name
                    : "<Scene inválida>";

                DebugUtility.LogVerbose<EventSystemDuplicateChecker>(
                    $"[UI] EventSystem único OK: name='{system.gameObject.name}', scene='{sceneName}'.",
                    DebugUtility.Colors.Info,
                    system.gameObject);
                return;
            }

            DebugUtility.LogWarning<EventSystemDuplicateChecker>(
                "[UI] Nenhum EventSystem ativo foi encontrado.");
        }
    }
}
