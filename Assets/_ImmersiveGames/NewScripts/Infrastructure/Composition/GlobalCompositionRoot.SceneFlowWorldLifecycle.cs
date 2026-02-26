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
using UnityEngine;

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
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] SceneTransitionService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Loader/Fade (NewScripts standalone)
            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);

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

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[SceneFlow] ISceneTransitionCompletionGate registrado ({completionGate.GetType().Name}, inner={innerGate.GetType().Name}).",
                    DebugUtility.Colors.Info);
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
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] INavigationPolicy registrado (AllowAllNavigationPolicy).",
                    DebugUtility.Colors.Info);
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
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] IRouteGuard registrado (AllowAllRouteGuard).",
                    DebugUtility.Colors.Info);
            }

            var routeResolver = ResolveOrRegisterRouteResolverRequired();

            var service = new SceneTransitionService(loaderAdapter, fadeAdapter, completionGate, navigationPolicy, routeResolver, routeGuard);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] SceneTransitionService nativo registrado (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}, Policy={navigationPolicy.GetType().Name}, RouteResolver={(routeResolver == null ? "None" : routeResolver.GetType().Name)}, RouteGuard={routeGuard.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowSignatureCache()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] ISceneFlowSignatureCache já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ISceneFlowSignatureCache>(
                new SceneFlowSignatureCache());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] SceneFlowSignatureCache registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowRouteResetPolicy()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRouteResetPolicy>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] IRouteResetPolicy já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var routeResolver = ResolveOrRegisterRouteResolverRequired();

            DependencyManager.Provider.RegisterGlobal<IRouteResetPolicy>(
                new SceneRouteResetPolicy(routeResolver));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] IRouteResetPolicy registrado (SceneRouteResetPolicy).",
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
