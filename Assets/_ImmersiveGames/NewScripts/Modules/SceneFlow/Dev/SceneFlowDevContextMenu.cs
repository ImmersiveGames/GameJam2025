#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
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

        private const string ReasonEnterGameplay = "QA/SceneFlow/EnterGameplay";
        private const string QaStartLevelId = "level.1";
        private const string ReasonQaRestart = "QA/PostGame/Restart";
        private const string ReasonQaExitToMenu = "QA/PostGame/ExitToMenu";
        private const string ReasonForceReset = "QA/WorldLifecycle/ForceResetWorld";
        private static readonly string[] RelevantRouteTokens = { "menu", "gameplay", "postgame", "restart", "exit" };

        [ContextMenu("QA/SceneFlow/EnterGameplay (TC: Menu->Gameplay ResetWorld)")]
        private void Qa_EnterGameplay()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][Navigation] QA EnterGameplay -> StartGameplayAsync levelId='{LevelId.Normalize(QaStartLevelId)}' reason='{ReasonEnterGameplay}'.",
                ColorInfo);

            _ = navigation.StartGameplayAsync(LevelId.FromName(QaStartLevelId), ReasonEnterGameplay);
        }


        [ContextMenu("QA/PostGame/Restart (TC: Restart Route)")]
        private void Qa_RestartNavigation()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][Navigation] QA Restart -> RestartAsync reason='{ReasonQaRestart}'.",
                ColorInfo);

            _ = navigation.RestartAsync(ReasonQaRestart);
        }

        [ContextMenu("QA/PostGame/ExitToMenu (TC: Frontend Route)")]
        private void Qa_ExitToMenuNavigation()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][Navigation] QA ExitToMenu -> ExitToMenuAsync reason='{ReasonQaExitToMenu}'.",
                ColorInfo);

            _ = navigation.ExitToMenuAsync(ReasonQaExitToMenu);
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
            if (!TryGetRouteCatalogAsset(out var routeCatalogAsset, out var sourceLabel))
            {
                return;
            }

            var routes = routeCatalogAsset.DebugGetRoutesSnapshot();
            int unspecifiedCount = 0;
            int totalRoutes = 0;
            var relevant = new List<RouteDumpItem>();
            var unspecifiedRelevant = new List<string>();

            for (int i = 0; i < routes.Count; i++)
            {
                var routeId = routes[i].RouteId;
                var routeDefinition = routes[i].RouteDefinition;

                totalRoutes++;
                if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
                {
                    unspecifiedCount++;
                }

                if (IsRelevant(routeId.Value))
                {
                    relevant.Add(new RouteDumpItem(routeId.Value, routeDefinition.RouteKind, routeDefinition.TargetActiveScene));
                    if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
                    {
                        unspecifiedRelevant.Add(routeId.Value);
                    }
                }
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][SceneFlow] Dump Route Catalog (RouteKind): totalRoutes={totalRoutes}, unspecified={unspecifiedCount}, source='{sourceLabel}'.",
                ColorInfo);

            foreach (var route in relevant)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] Route routeId='{route.RouteId}', routeKind='{route.RouteKind}', targetActiveScene='{route.TargetActiveScene}'.",
                    ColorInfo);
            }

            if (unspecifiedRelevant.Count > 0)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] Rotas relevantes ainda com routeKind=Unspecified: {string.Join(", ", unspecifiedRelevant)}.",
                    ColorErr);
            }
        }

        private static bool TryGetRouteCatalogAsset(out SceneRouteCatalogAsset routeCatalogAsset, out string sourceLabel)
        {
            routeCatalogAsset = null;
            sourceLabel = string.Empty;

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<ISceneRouteCatalog>(out var catalogFromDi) &&
                catalogFromDi is SceneRouteCatalogAsset catalogAssetFromDi)
            {
                routeCatalogAsset = catalogAssetFromDi;
                sourceLabel = "DI/ISceneRouteCatalog(SceneRouteCatalogAsset)";
                return true;
            }

            const string resourcesPath = "SceneFlow/SceneRouteCatalog";
            var catalogFromResources = Resources.Load<SceneRouteCatalogAsset>(resourcesPath);
            if (catalogFromResources != null)
            {
                routeCatalogAsset = catalogFromResources;
                sourceLabel = $"Resources/{resourcesPath}";
                return true;
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][SceneFlow] Dump Route Catalog (RouteKind): SceneRouteCatalogAsset não encontrado via DI cast nem Resources (path='{resourcesPath}').",
                ColorErr);
            return false;
        }

        private static bool IsRelevant(string routeId)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            var normalized = routeId.Trim().ToLowerInvariant();
            for (int i = 0; i < RelevantRouteTokens.Length; i++)
            {
                if (normalized.Contains(RelevantRouteTokens[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct RouteDumpItem
        {
            public RouteDumpItem(string routeId, SceneRouteKind routeKind, string targetActiveScene)
            {
                RouteId = routeId;
                RouteKind = routeKind;
                TargetActiveScene = targetActiveScene;
            }

            public string RouteId { get; }
            public SceneRouteKind RouteKind { get; }
            public string TargetActiveScene { get; }
        }
#endif
    }
}
