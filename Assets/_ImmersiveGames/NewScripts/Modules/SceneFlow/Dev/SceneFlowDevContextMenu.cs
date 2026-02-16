#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Dev
{
    public sealed class SceneFlowDevContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorErr = "#F44336";

        private const string ReasonEnterGameplay = "QA/StartGameplay";
        private const string QaStartLevelId = "level.1";
        private const string ReasonQaRestart = "QA/RestartGameplay";
        private const string ReasonQaExitToMenu = "QA/ExitToMenu";
        private const string ReasonForceReset = "QA/WorldLifecycle/ForceResetWorld";

        [ContextMenu("QA/StartGameplay (levelId)")]
        private void Qa_EnterGameplay()
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            if (levelFlow == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][LevelFlow] QA EnterGameplay -> StartGameplayAsync levelId='{LevelId.Normalize(QaStartLevelId)}' reason='{ReasonEnterGameplay}'.",
                ColorInfo);

            _ = levelFlow.StartGameplayAsync(QaStartLevelId, ReasonEnterGameplay);
        }


        [ContextMenu("QA/RestartGameplay (levelId)")]
        private void Qa_RestartNavigation()
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            if (levelFlow == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][LevelFlow] QA Restart -> StartGameplayAsync levelId='{LevelId.Normalize(QaStartLevelId)}' reason='{ReasonQaRestart}'.",
                ColorInfo);

            _ = levelFlow.StartGameplayAsync(QaStartLevelId, ReasonQaRestart);
        }

        [ContextMenu("QA/ExitToMenu")]
        private void Qa_ExitToMenuNavigation()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][Navigation] QA ExitToMenu -> NavigateAsync(core=Menu) reason='{ReasonQaExitToMenu}'.",
                ColorInfo);

            _ = navigation.NavigateAsync(GameNavigationIntentKind.Menu, ReasonQaExitToMenu);
        }

        [ContextMenu("QA/WorldLifecycle/ForceResetWorld (TC: Manual ResetWorld)")]
        private void Qa_ForceResetWorld()
        {
            var resetService = ResolveGlobal<IWorldResetRequestService>("IWorldResetRequestService");
            if (resetService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                "[QA][WorldLifecycle] Forçando ResetWorld via serviço global.",
                ColorInfo);

            _ = resetService.RequestResetAsync(ReasonForceReset);
        }

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    "[QA][SceneFlow] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[QA][SceneFlow] Serviço global ausente: {label}.",
                    ColorErr);
                return null;
            }

            return service;
        }

#if UNITY_EDITOR
        [ContextMenu("QA/SceneFlow/Dump Route Catalog (RouteKind)")]
        private void Qa_DumpRouteCatalogRouteKind()
        {
            var routeCatalogAsset = GetRouteCatalogAssetOrFail(out var sourceLabel);

            var routes = routeCatalogAsset.DebugGetRoutesSnapshot();
            int unspecifiedCount = 0;
            int totalRoutes = 0;
            var policyAnnotatedRoutes = new List<RouteDumpItem>();
            var routeKindCounts = new Dictionary<SceneRouteKind, int>();
            int requiresWorldResetCount = 0;

            for (int i = 0; i < routes.Count; i++)
            {
                var routeId = routes[i].RouteId;
                var routeDefinition = routes[i].RouteDefinition;

                totalRoutes++;
                if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
                {
                    unspecifiedCount++;
                }

                TrackRouteKind(routeKindCounts, routeDefinition.RouteKind);
                if (routeDefinition.RequiresWorldReset)
                {
                    requiresWorldResetCount++;
                }

                policyAnnotatedRoutes.Add(new RouteDumpItem(routeId, routeDefinition.RouteKind, routeDefinition.TargetActiveScene, routeDefinition.RequiresWorldReset));
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][SceneFlow] Dump Route Catalog (RouteKind): totalRoutes={totalRoutes}, unspecified={unspecifiedCount}, requiresWorldReset={requiresWorldResetCount}, source='{sourceLabel}'.",
                ColorInfo);

            foreach (var pair in routeKindCounts)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] RouteKindSummary kind='{pair.Key}' count={pair.Value}.",
                    ColorInfo);
            }

            foreach (var route in policyAnnotatedRoutes)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] Route routeId='{route.RouteId}', routeKind='{route.RouteKind}', requiresWorldReset={route.RequiresWorldReset}, targetActiveScene='{route.TargetActiveScene}'.",
                    ColorInfo);
            }

            if (unspecifiedCount > 0)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    "[OBS][SceneFlow] Existem rotas com routeKind=Unspecified; revisar policy metadata no catálogo.",
                    ColorErr);
            }
        }

        private static SceneRouteCatalogAsset GetRouteCatalogAssetOrFail(out string sourceLabel)
        {
            sourceLabel = string.Empty;

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<SceneRouteCatalogAsset>(out var routeCatalogAssetFromGlobal) &&
                routeCatalogAssetFromGlobal != null)
            {
                sourceLabel = "DI/SceneRouteCatalogAsset";
                return routeCatalogAssetFromGlobal;
            }

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var bootstrapConfig) &&
                bootstrapConfig != null &&
                bootstrapConfig.SceneRouteCatalog != null)
            {
                sourceLabel = "DI/NewScriptsBootstrapConfigAsset.SceneRouteCatalog";
                return bootstrapConfig.SceneRouteCatalog;
            }

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<ISceneRouteCatalog>(out var catalogFromDi) &&
                catalogFromDi is SceneRouteCatalogAsset catalogAssetFromDi)
            {
                sourceLabel = "DI/ISceneRouteCatalog(SceneRouteCatalogAsset)";
                return catalogAssetFromDi;
            }

            const string errorMessage =
                "[OBS][Config] Dump Route Catalog (RouteKind): SceneRouteCatalogAsset indisponível (DI-only). " +
                "Configure NewScriptsBootstrapConfigAsset.SceneRouteCatalog e garanta o registro de SceneRouteCatalogAsset, " +
                "NewScriptsBootstrapConfigAsset ou ISceneRouteCatalog no GlobalCompositionRoot.";

            DebugUtility.Log(typeof(SceneFlowDevContextMenu), errorMessage, ColorErr);
            throw new InvalidOperationException(errorMessage);
        }

        private static void TrackRouteKind(IDictionary<SceneRouteKind, int> counts, SceneRouteKind routeKind)
        {
            if (counts.TryGetValue(routeKind, out int current))
            {
                counts[routeKind] = current + 1;
                return;
            }

            counts[routeKind] = 1;
        }

        private readonly struct RouteDumpItem
        {
            public RouteDumpItem(SceneRouteId routeId, SceneRouteKind routeKind, string targetActiveScene, bool requiresWorldReset)
            {
                RouteId = routeId;
                RouteKind = routeKind;
                TargetActiveScene = targetActiveScene;
                RequiresWorldReset = requiresWorldReset;
            }

            public SceneRouteId RouteId { get; }
            public SceneRouteKind RouteKind { get; }
            public string TargetActiveScene { get; }
            public bool RequiresWorldReset { get; }
        }
#endif
    }
}
