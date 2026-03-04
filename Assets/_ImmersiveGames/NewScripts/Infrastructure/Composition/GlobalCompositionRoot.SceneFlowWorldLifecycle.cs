using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // SceneFlow / WorldLifecycle
        // --------------------------------------------------------------------

        private static void RegisterSceneFlowNative()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                return;
            }

            // Loader/Fade (NewScripts standalone)
            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);

            int gateAdded = 0;
            int policyAdded = 0;
            int routeGuardAdded = 0;

            // Gate composto: (1) WorldLifecycle reset -> (2) LevelPrepare (macro gameplay) -> libera FadeOut.
            ISceneTransitionCompletionGate completionGate = null;
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                completionGate = existingGate;
            }

            if (completionGate is not MacroLevelPrepareCompletionGate)
            {
                WorldLifecycleResetCompletionGate innerGate = completionGate as WorldLifecycleResetCompletionGate;
                if (innerGate == null)
                {
                    if (completionGate != null)
                    {
                        DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                            $"[SceneFlow] ISceneTransitionCompletionGate não é WorldLifecycleResetCompletionGate (tipo='{completionGate.GetType().Name}'). Substituindo para cumprir o contrato SceneFlow/WorldLifecycle (completion gate).");
                    }

                    innerGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                }

                completionGate = new MacroLevelPrepareCompletionGate(innerGate);
                DependencyManager.Provider.RegisterGlobal(completionGate);
                gateAdded = 1;
            }

            INavigationPolicy navigationPolicy = null;
            if (DependencyManager.Provider.TryGetGlobal<INavigationPolicy>(out var existingPolicy) && existingPolicy != null)
            {
                navigationPolicy = existingPolicy;
            }
            else
            {
                navigationPolicy = new AllowAllNavigationPolicy();
                DependencyManager.Provider.RegisterGlobal(navigationPolicy);
                policyAdded = 1;
            }

            IRouteGuard routeGuard = null;
            if (DependencyManager.Provider.TryGetGlobal<IRouteGuard>(out var existingRouteGuard) && existingRouteGuard != null)
            {
                routeGuard = existingRouteGuard;
            }
            else
            {
                routeGuard = new AllowAllRouteGuard();
                DependencyManager.Provider.RegisterGlobal(routeGuard);
                routeGuardAdded = 1;
            }

            var routeResolver = ResolveOrRegisterRouteResolverRequired();

            var service = new SceneTransitionService(loaderAdapter, fadeAdapter, completionGate, navigationPolicy, routeResolver, routeGuard);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] RegisterSceneFlowNative summary: transitionServiceAdded=1, gateAdded={gateAdded}, policyAdded={policyAdded}, routeGuardAdded={routeGuardAdded}, resolver='{routeResolver.GetType().Name}'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowSignatureCache()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISceneFlowSignatureCache>(
                    new SceneFlowSignatureCache());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] RegisterSceneFlowSignatureCache summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowRouteResetPolicy()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IRouteResetPolicy>(out var existing) || existing == null)
            {
                var routeResolver = ResolveOrRegisterRouteResolverRequired();

                DependencyManager.Provider.RegisterGlobal<IRouteResetPolicy>(
                    new SceneRouteResetPolicy(routeResolver));
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] RegisterSceneFlowRouteResetPolicy summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static ISceneRouteResolver ResolveOrRegisterRouteResolverRequired()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var existingResolver) && existingResolver != null)
            {
                return existingResolver;
            }
            throw new InvalidOperationException(
                "[SceneFlow] ISceneRouteResolver obrigatório ausente no DI global. " +
                "Garanta a execução de RegisterSceneFlowRoutesRequired no pipeline antes de RegisterSceneFlowNative.");
        }

        // --------------------------------------------------------------------
    }
}
