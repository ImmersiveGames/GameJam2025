using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Utilitário centralizado para localizar <see cref="WorldLifecycleController"/>.
    ///
    /// Porque existe:
    /// - Reduz duplicidade (driver do SceneFlow e serviço de request usavam lógica similar).
    /// - Padroniza o comportamento de fallback (ex.: cenário com 1 controller total).
    /// - Mantém a filtragem por cena carregada (evita controllers órfãos/descarregando).
    /// </summary>
    internal static class WorldLifecycleControllerLocator
    {
        public static IReadOnlyList<WorldLifecycleController> FindControllersForScene(string sceneName)
        {
            string target = (sceneName ?? string.Empty).Trim();
            IReadOnlyList<WorldLifecycleController> all = FindAllControllers(includeInactive: true);

            if (all.Count == 0)
            {
                return Array.Empty<WorldLifecycleController>();
            }

            if (target.Length == 0)
            {
                // Sem alvo → retorna todos válidos.
                return all;
            }

            List<WorldLifecycleController> result = null;
            foreach (var c in all)
            {
                if (c == null)
                {
                    continue;
                }

                var scene = c.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (!string.Equals(scene.name, target, StringComparison.Ordinal))
                {
                    continue;
                }

                result ??= new List<WorldLifecycleController>(1);
                result.Add(c);
            }

            return result ?? (IReadOnlyList<WorldLifecycleController>)Array.Empty<WorldLifecycleController>();
        }

        public static WorldLifecycleController FindSingleForSceneOrFallback(string sceneName)
        {
            string target = (sceneName ?? string.Empty).Trim();

            // 1) tenta localizar por nome da cena.
            if (target.Length > 0)
            {
                IReadOnlyList<WorldLifecycleController> list = FindControllersForScene(target);
                if (list.Count == 1)
                {
                    return list[0];
                }

                // Se houver múltiplos, preferimos não adivinhar.
                if (list.Count > 1)
                {
                    return null;
                }
            }

            // 2) fallback: se houver exatamente um controller válido em todas as cenas carregadas.
            IReadOnlyList<WorldLifecycleController> all = FindAllControllers(includeInactive: true);
            return all.Count == 1 ? all[0] : null;
        }

        public static string ResolveTargetSceneNameFromActiveIfMissing(string requestedSceneName)
        {
            string target = (requestedSceneName ?? string.Empty).Trim();
            if (target.Length > 0)
            {
                return target;
            }

            // Preferência: ActiveScene (é o comportamento mais intuitivo para requests manuais).
            var active = SceneManager.GetActiveScene();
            return (active.IsValid() ? active.name : string.Empty) ?? string.Empty;
        }

        private static IReadOnlyList<WorldLifecycleController> FindAllControllers(bool includeInactive)
        {
            // Unity 6/2022+: API recomendada.
            var inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            WorldLifecycleController[] all = UnityEngine.Object.FindObjectsByType<WorldLifecycleController>(inactive, FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<WorldLifecycleController>();
            }

            // Filtra por cena válida e carregada para reduzir falso-positivo durante unload.
            List<WorldLifecycleController> valid = null;
            foreach (var c in all)
            {
                if (c == null)
                {
                    continue;
                }

                var scene = c.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                valid ??= new List<WorldLifecycleController>(all.Length);
                valid.Add(c);
            }

            return valid ?? (IReadOnlyList<WorldLifecycleController>)Array.Empty<WorldLifecycleController>();
        }
    }
}


