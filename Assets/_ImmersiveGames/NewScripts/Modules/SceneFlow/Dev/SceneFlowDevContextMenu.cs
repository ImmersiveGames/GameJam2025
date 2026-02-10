#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
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
                "[QA][SceneFlow] Solicitação de transição Menu -> Gameplay enviada.",
                ColorInfo);

            _ = navigation.RestartAsync(ReasonEnterGameplay);
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

        [ContextMenu("QA/SceneFlow/Dump Route Catalog (RouteKind)")]
        private void Qa_DumpRouteCatalogRouteKind()
        {
            const string resourcesPath = "SceneFlow/SceneRouteCatalog";
            var catalog = Resources.Load<SceneRouteCatalogAsset>(resourcesPath);

            if (catalog == null)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] Dump Route Catalog (RouteKind): asset não encontrado via Resources (path='{resourcesPath}').",
                    ColorErr);
                return;
            }

            if (!TryReadRoutes(catalog, out var routes))
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    "[OBS][SceneFlow] Dump Route Catalog (RouteKind): falha ao inspecionar lista de rotas no asset.",
                    ColorErr);
                return;
            }

            int unspecifiedCount = 0;
            var relevant = new List<RouteDumpItem>();

            foreach (var route in routes)
            {
                if (route.RouteKind == SceneRouteKind.Unspecified)
                {
                    unspecifiedCount++;
                }

                if (IsRelevant(route.RouteId))
                {
                    relevant.Add(route);
                }
            }

            DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                $"[OBS][SceneFlow] Dump Route Catalog (RouteKind): totalRoutes={routes.Count}, unspecified={unspecifiedCount}, resourcesPath='{resourcesPath}'.",
                ColorInfo);

            foreach (var route in relevant)
            {
                DebugUtility.Log(typeof(SceneFlowDevContextMenu),
                    $"[OBS][SceneFlow] Route routeId='{route.RouteId}', routeKind='{route.RouteKind}', targetActiveScene='{route.TargetActiveScene}'.",
                    ColorInfo);
            }
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

        // Leitura por reflexão para manter o helper DEV sem acoplamento no caminho crítico.
        private static bool TryReadRoutes(SceneRouteCatalogAsset catalog, out List<RouteDumpItem> routes)
        {
            routes = new List<RouteDumpItem>();
            if (catalog == null)
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var routesField = typeof(SceneRouteCatalogAsset).GetField("routes", flags);
            if (routesField == null)
            {
                return false;
            }

            if (routesField.GetValue(catalog) is not IEnumerable enumerable)
            {
                return false;
            }

            foreach (var entry in enumerable)
            {
                if (entry == null)
                {
                    continue;
                }

                var entryType = entry.GetType();
                var routeIdField = entryType.GetField("routeId", flags);
                var routeKindField = entryType.GetField("routeKind", flags);
                var targetActiveSceneField = entryType.GetField("targetActiveScene", flags);

                var routeId = routeIdField?.GetValue(entry)?.ToString() ?? string.Empty;
                var targetActiveScene = targetActiveSceneField?.GetValue(entry)?.ToString() ?? string.Empty;

                var routeKind = SceneRouteKind.Unspecified;
                if (routeKindField?.GetValue(entry) is SceneRouteKind parsedRouteKind)
                {
                    routeKind = parsedRouteKind;
                }
                else if (routeKindField?.GetValue(entry) is int rawKind && Enum.IsDefined(typeof(SceneRouteKind), rawKind))
                {
                    routeKind = (SceneRouteKind)rawKind;
                }

                routes.Add(new RouteDumpItem(routeId, routeKind, targetActiveScene));
            }

            return true;
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
    }
}

