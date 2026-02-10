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

            // Gate para segurar FadeOut/Completed até WorldLifecycle reset concluir.
            ISceneTransitionCompletionGate completionGate = null;
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                completionGate = existingGate;
            }

            if (completionGate is not WorldLifecycleResetCompletionGate)
            {
                if (completionGate != null)
                {
                    DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                        $"[SceneFlow] ISceneTransitionCompletionGate não é WorldLifecycleResetCompletionGate (tipo='{completionGate.GetType().Name}'). Substituindo para cumprir o contrato SceneFlow/WorldLifecycle (completion gate).");
                }

                completionGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                DependencyManager.Provider.RegisterGlobal(completionGate);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] ISceneTransitionCompletionGate registrado (WorldLifecycleResetCompletionGate).",
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

            var routeResolver = ResolveOrRegisterRouteResolverBestEffort();

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

            var routeResolver = ResolveOrRegisterRouteResolverBestEffort();

            DependencyManager.Provider.RegisterGlobal<IRouteResetPolicy>(
                new SceneRouteResetPolicy(routeResolver));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] IRouteResetPolicy registrado (SceneRouteResetPolicy).",
                DebugUtility.Colors.Info);
        }

        private static ISceneRouteResolver ResolveOrRegisterRouteResolverBestEffort()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var existingResolver) && existingResolver != null)
            {
                return existingResolver;
            }

            if (DependencyManager.Provider.TryGetGlobal<ISceneRouteCatalog>(out var routeCatalog) && routeCatalog != null)
            {
                var resolverFromDiCatalog = new SceneRouteCatalogResolver(routeCatalog);
                DependencyManager.Provider.RegisterGlobal<ISceneRouteResolver>(resolverFromDiCatalog);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][SceneFlow] ISceneRouteResolver não estava registrado; criado automaticamente a partir de ISceneRouteCatalog.",
                    DebugUtility.Colors.Info);

                return resolverFromDiCatalog;
            }

            const string sceneRouteCatalogResourcesPath = "SceneFlow/SceneRouteCatalog";
            var catalogFromResources = Resources.Load<SceneRouteCatalogAsset>(sceneRouteCatalogResourcesPath);

            if (catalogFromResources != null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteCatalog>(out routeCatalog) || routeCatalog == null)
                {
                    routeCatalog = catalogFromResources;
                    DependencyManager.Provider.RegisterGlobal<ISceneRouteCatalog>(routeCatalog);
                }

                var resolverFromResources = new SceneRouteCatalogResolver(routeCatalog);
                DependencyManager.Provider.RegisterGlobal<ISceneRouteResolver>(resolverFromResources);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][SceneFlow] ISceneRouteCatalog/ISceneRouteResolver carregados via Resources antes do Navigation " +
                    $"(path='{sceneRouteCatalogResourcesPath}').",
                    DebugUtility.Colors.Info);

                return resolverFromResources;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][SceneFlow] ISceneRouteResolver ausente durante bootstrap do SceneFlow e catálogo não encontrado via Resources; " +
                "SceneTransitionService seguirá sem hidratação de payload por rota até o resolver estar disponível.",
                DebugUtility.Colors.Info);

            return null;
        }

        // --------------------------------------------------------------------
    }
}
