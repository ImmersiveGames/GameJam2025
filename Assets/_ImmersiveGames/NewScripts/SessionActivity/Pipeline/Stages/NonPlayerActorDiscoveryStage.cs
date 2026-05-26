using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class NonPlayerActorDiscoveryStage
    {
        public static SessionActivityPipeline.NonPlayerActorDiscoveryStageResult Execute(
            SessionActivityDefinition definition,
            SessionActivityIdentity identity,
            ActivityContentLoadedSet loadedSet,
            bool canDiscoverFromLoadedSet,
            ActivityNonPlayerActorRegistry registry)
        {
            bool hasAuthorizedSource = false;
            List<SessionActivityPipeline.NonPlayerActorDiscoveredRecord> discovered = new();
            if (canDiscoverFromLoadedSet && loadedSet.HasScenes)
            {
                hasAuthorizedSource = true;
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    ActivityContentLoadedSceneRecord record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException($"Invalid loaded scene record at index='{sceneIndex}' for non-player actor discovery.");
                    }

                    Scene contentScene = SceneManager.GetSceneByName(record.SceneName);
                    if (!contentScene.IsValid() || !contentScene.isLoaded)
                    {
                        throw new InvalidOperationException($"Non-player actor discovery requires loaded scene='{record.SceneName}' activityId='{definition.ActivityId}'.");
                    }

                    DiscoverInScene(identity, contentScene, NonPlayerActorOriginSource.ActivityContent, NonPlayerActorScope.ActivityScoped, registry, discovered);
                }
            }

            Scene routeScene = SceneManager.GetActiveScene();
            if (routeScene.IsValid() && routeScene.isLoaded)
            {
                hasAuthorizedSource = true;
                DiscoverInScene(identity, routeScene, NonPlayerActorOriginSource.RouteScene, NonPlayerActorScope.RouteScoped, registry, discovered);
            }

            return new SessionActivityPipeline.NonPlayerActorDiscoveryStageResult(hasAuthorizedSource, discovered);
        }

        private static void DiscoverInScene(
            SessionActivityIdentity identity,
            Scene sourceScene,
            NonPlayerActorOriginSource originSource,
            NonPlayerActorScope expectedScope,
            ActivityNonPlayerActorRegistry registry,
            List<SessionActivityPipeline.NonPlayerActorDiscoveredRecord> discovered)
        {
            GameObject[] roots = sourceScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                NonPlayerActorEndpoint[] endpoints = roots[rootIndex].GetComponentsInChildren<NonPlayerActorEndpoint>(true);
                for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
                {
                    NonPlayerActorEndpoint endpoint = endpoints[endpointIndex];
                    if (endpoint == null)
                    {
                        continue;
                    }

                    endpoint.ValidateOrThrow($"NonPlayerActorDiscovery:{sourceScene.name}:{rootIndex}:{endpointIndex}");
                    if (endpoint.ActorScope == NonPlayerActorScope.GlobalScopedUnsupported)
                    {
                        throw new InvalidOperationException($"NonPlayerActorEndpoint '{endpoint.name}' uses unsupported actorScope='GlobalScopedUnsupported'.");
                    }

                    if (endpoint.ActorScope != expectedScope)
                    {
                        continue;
                    }

                    NonPlayerActorIdentityRecord resolvedIdentity = new(
                        identity,
                        endpoint.NonPlayerActorId,
                        endpoint.ActorKind,
                        endpoint.ActorScope,
                        endpoint.ParticipationPolicy,
                        endpoint.ResolveParticipatingActivityIdsOrFail($"NonPlayerActorDiscovery:{sourceScene.name}:{rootIndex}:{endpointIndex}"),
                        originSource,
                        sourceScene.name);
                    registry.RegisterDiscovered(resolvedIdentity, endpoint, endpoint.gameObject);
                    discovered.Add(new SessionActivityPipeline.NonPlayerActorDiscoveredRecord(resolvedIdentity));
                }
            }
        }
    }
}
