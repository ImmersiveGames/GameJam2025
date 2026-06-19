using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class PauseOverlayAdapter : ISessionActivityPauseOverlayAdapter
    {
        public void Show(
            SessionActivityIdentity identity,
            SessionActivityRoutePauseSurfaceContext context,
            string source,
            string reason)
        {
            var endpoint = ResolveEndpointOrFail(context, nameof(Show), source, reason);
            endpoint.Show(identity, context, source, reason);

            DebugUtility.Log(typeof(PauseOverlayAdapter),
                $"action='Show' event='PauseOverlayAdapterApplied' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' overlayRootId='{context.OverlayRootId}' identity='{identity}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        public void Hide(
            SessionActivityIdentity identity,
            SessionActivityRoutePauseSurfaceContext context,
            string source,
            string reason)
        {
            var endpoint = ResolveEndpointOrFail(context, nameof(Hide), source, reason);
            endpoint.Hide(identity, context, source, reason);

            DebugUtility.Log(typeof(PauseOverlayAdapter),
                $"action='Hide' event='PauseOverlayAdapterApplied' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' overlayRootId='{context.OverlayRootId}' identity='{identity}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        private static RoutePauseSurfaceEndpoint ResolveEndpointOrFail(
            SessionActivityRoutePauseSurfaceContext context,
            string operation,
            string source,
            string reason)
        {
            if (!context.HasSurface || !context.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] RoutePauseSurface context obrigatorio ausente ou invalido operation='{operation}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            var scene = SceneManager.GetSceneByName(context.SceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] RoutePauseSurface scene nao carregada operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            RoutePauseSurfaceEndpoint resolved = null;
            int matchCount = 0;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var endpoints = root.GetComponentsInChildren<RoutePauseSurfaceEndpoint>(true);
                for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
                {
                    var endpoint = endpoints[endpointIndex];
                    if (endpoint == null || !endpoint.Matches(context))
                    {
                        continue;
                    }

                    resolved = endpoint;
                    matchCount += 1;
                }
            }

            if (matchCount == 0 || resolved == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] RoutePauseSurfaceEndpoint nao encontrado operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' overlayRootId='{context.OverlayRootId}' activityContentRootId='{context.ActivityContentRootId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            if (matchCount > 1)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseSurface] RoutePauseSurfaceEndpoint duplicado operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' matchCount='{matchCount}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            DebugUtility.LogVerbose(typeof(PauseOverlayAdapter),
                $"event='RoutePauseSurfaceEndpointResolved' operation='{operation}' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' overlayRootId='{context.OverlayRootId}' activityContentRootId='{context.ActivityContentRootId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);

            return resolved;
        }
    }
}
