#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Dev;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public sealed partial class SceneScopeCompositionRoot
    {
        private void RegisterSceneLifecycleHooksDevQa(
            WorldLifecycleHookRegistry hookRegistry,
            Transform worldRoot)
        {
            if (hookRegistry == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "WorldLifecycleHookRegistry ausente; hooks de cena não serão registrados.");
                return;
            }

            if (worldRoot == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "WorldRoot ausente; hooks de cena não serão adicionados.");
                return;
            }

            /* Aqui é ume exemplo de hook no ciclo do mundo.*/
            var hookA = EnsureHookComponent<WorldLifecycleHookLoggerA>(worldRoot);
            RegisterHookIfMissing(hookRegistry, hookA);
        }

        private static T EnsureHookComponent<T>(Transform worldRoot)
            where T : Component
        {
            var existing = worldRoot.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return worldRoot.gameObject.AddComponent<T>();
        }

        private static void RegisterHookIfMissing(
            WorldLifecycleHookRegistry registry,
            IWorldLifecycleHook hook)
        {
            if (hook == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "Hook de cena nulo; registro ignorado.");
                return;
            }

            if (registry.Hooks.Contains(hook))
            {
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"Hook de cena já registrado: {hook.GetType().Name}");
                return;
            }

            registry.Register(hook);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"Hook de cena registrado: {hook.GetType().Name}");
        }
    }
}
#endif

