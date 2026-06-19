using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class ActivityPauseContentAdapter : ISessionActivityPauseContentAdapter
    {
        public ActivityPauseContentBindingResult Bind(ActivityPauseContentBindingCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityPauseContentBindingCommand is invalid.");
            }

            var endpoint = ResolveEndpointOrFail(command.RoutePauseSurfaceContext, nameof(Bind), command.Source, command.Reason);
            var result = endpoint.BindActivityPauseContent(command);

            DebugUtility.Log(typeof(ActivityPauseContentAdapter),
                $"event='ActivityPauseContentBound' surfaceId='{command.RoutePauseSurfaceContext.SurfaceId}' sceneName='{command.RoutePauseSurfaceContext.SceneName}' activityId='{command.ActivityId}' entrySequence='{command.EntrySequence}' profileId='{command.Profile.ProfileId}' requestedCount='{result.RequestedCount}' boundCount='{result.BoundCount}' skippedCount='{result.SkippedCount}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            return result;
        }

        public ActivityPauseContentReleaseResult Release(ActivityPauseContentReleaseCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityPauseContentReleaseCommand is invalid.");
            }

            var endpoint = ResolveEndpointOrFail(command.RoutePauseSurfaceContext, nameof(Release), command.Source, command.Reason);
            var result = endpoint.ReleaseActivityPauseContent(command);

            DebugUtility.Log(typeof(ActivityPauseContentAdapter),
                $"event='ActivityPauseContentReleased' surfaceId='{command.RoutePauseSurfaceContext.SurfaceId}' sceneName='{command.RoutePauseSurfaceContext.SceneName}' activityId='{command.Identity.ActivityId}' entrySequence='{command.Identity.EntrySequence}' profileId='{command.CurrentBinding.ProfileId}' releasedCount='{result.ReleasedCount}' skipped='{result.Skipped.ToString().ToLowerInvariant()}' source='{command.Source}' reason='{command.Reason}'.",
                result.Skipped ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);
            return result;
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
                    $"[FATAL][PauseContent] RoutePauseSurface context obrigatorio ausente ou invalido operation='{operation}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            var scene = SceneManager.GetSceneByName(context.SceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] RoutePauseSurface scene nao carregada operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
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
                    $"[FATAL][PauseContent] RoutePauseSurfaceEndpoint nao encontrado operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' activityContentRootId='{context.ActivityContentRootId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            if (matchCount > 1)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] RoutePauseSurfaceEndpoint duplicado operation='{operation}' sceneName='{context.SceneName}' surfaceId='{context.SurfaceId}' matchCount='{matchCount}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }

            DebugUtility.LogVerbose(typeof(ActivityPauseContentAdapter),
                $"event='RoutePauseSurfaceEndpointResolvedForPauseContent' operation='{operation}' surfaceId='{context.SurfaceId}' sceneName='{context.SceneName}' activityContentRootId='{context.ActivityContentRootId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);
            return resolved;
        }
    }
}
