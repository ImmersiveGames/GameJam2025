using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.Bootstrap
{
    /// <summary>
    /// Minimal invoker to ensure Gameplay runtime bridges are composed when the
    /// composition runner does not call the Gameplay bootstrap early enough.
    ///
    /// This is a conservative, fail-fast invoker that simply delegates to the
    /// canonical bootstrap. It does NOT register installers or alter any contracts.
    ///
    /// Comments in Portuguese: garante composição runtime mínima para bridges de actors.
    /// </summary>
    internal static class GameplayRuntimeBootstrapInvoker
    {
        // Run after assemblies loaded to avoid races with DI/provider initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InvokeIfNeeded()
        {
            try
            {
                // Ensure the DependencyManager and the crucial execution policy service are registered
                // before attempting to compose the gameplay runtime. This avoids early composition
                // attempts that fail because other bootstraps (e.g. ActorsSystemBootstrap) run later.
                // Comments in Portuguese: evita compor runtime antes dos serviços obrigatórios estarem prontos.

                var provider = DependencyManager.Provider;
                if (provider == null)
                {
                    Debug.Log("[GameplayRuntimeBootstrapInvoker] DependencyManager.Provider not ready; deferring gameplay runtime composition.");
                    return;
                }

                if (!provider.TryGetGlobal<IActorsMaterializationExecutionPolicyService>(out var executionPolicyService) || executionPolicyService == null)
                {
                    Debug.Log("[GameplayRuntimeBootstrapInvoker] IActorsMaterializationExecutionPolicyService not registered yet; deferring gameplay runtime composition.");
                    return;
                }

                // Safe to compose runtime now.
                GameplayRuntimeBootstrap.ComposeRuntime();
            }
            catch (System.Exception ex)
            {
                // Fail-fast for unexpected errors during composition.
                Debug.LogError($"[GameplayRuntimeBootstrapInvoker] failed to compose gameplay runtime: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
}

