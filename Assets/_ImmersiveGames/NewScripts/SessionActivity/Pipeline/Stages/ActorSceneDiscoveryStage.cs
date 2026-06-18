using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActorSceneDiscoveredRecord
    {
        public ActorSceneDiscoveredRecord(SceneAuthoredActorIdentityRecord identity)
        {
            Identity = identity;
        }

        public SceneAuthoredActorIdentityRecord Identity { get; }
        public bool IsValid => Identity.IsValid;
    }

    internal readonly struct ActorSceneDiscoveryStageResult
    {
        public ActorSceneDiscoveryStageResult(
            bool hasAuthorizedSource,
            IReadOnlyList<ActorSceneDiscoveredRecord> discoveredRecords)
        {
            HasAuthorizedSource = hasAuthorizedSource;
            DiscoveredRecords = discoveredRecords ?? Array.Empty<ActorSceneDiscoveredRecord>();
        }

        public bool HasAuthorizedSource { get; }
        public IReadOnlyList<ActorSceneDiscoveredRecord> DiscoveredRecords { get; }
        public int DiscoveredCount => DiscoveredRecords?.Count ?? 0;
    }

    internal static class ActorSceneDiscoveryStage
    {
        public static ActorSceneDiscoveryStageResult Execute(
            string activityId,
            SessionActivityIdentity identity,
            ActivityContentLoadedSet loadedSet,
            bool canDiscoverFromLoadedSet,
            ActivitySceneActorRegistry registry)
        {
            bool hasAuthorizedSource = false;
            List<ActorSceneDiscoveredRecord> discovered = new();
            if (canDiscoverFromLoadedSet && loadedSet.HasScenes)
            {
                hasAuthorizedSource = true;
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    var record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException($"Invalid loaded scene record at index='{sceneIndex}' for actor scene discovery.");
                    }

                    var contentScene = SceneManager.GetSceneByName(record.SceneName);
                    if (!contentScene.IsValid() || !contentScene.isLoaded)
                    {
                        throw new InvalidOperationException($"Actor scene discovery requires loaded scene='{record.SceneName}' activityId='{activityId}'.");
                    }

                    DiscoverInScene(identity, contentScene, ActorSourceKind.ActivityContent, ActorScope.ActivityScoped, registry, discovered);
                }
            }

            var routeScene = SceneManager.GetActiveScene();
            if (routeScene.IsValid() && routeScene.isLoaded)
            {
                hasAuthorizedSource = true;
                DiscoverInScene(identity, routeScene, ActorSourceKind.RouteScene, ActorScope.RouteScoped, registry, discovered);
            }

            return new ActorSceneDiscoveryStageResult(hasAuthorizedSource, discovered);
        }

        private static void DiscoverInScene(
            SessionActivityIdentity identity,
            Scene sourceScene,
            ActorSourceKind originSource,
            ActorScope expectedScope,
            ActivitySceneActorRegistry registry,
            List<ActorSceneDiscoveredRecord> discovered)
        {
            GameObject[] roots = sourceScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Actor[] actors = roots[rootIndex].GetComponentsInChildren<Actor>(true);
                for (int actorIndex = 0; actorIndex < actors.Length; actorIndex++)
                {
                    var actor = actors[actorIndex];
                    if (actor == null || actor is not ISceneAuthoredActor sceneAuthoredActor)
                    {
                        continue;
                    }

                    string context = $"ActorSceneDiscovery:{sourceScene.name}:{rootIndex}:{actorIndex}";
                    actor.ValidateLocalConfigurationOrThrow(context);
                    sceneAuthoredActor.ValidateSceneAuthoredConfigurationOrThrow(context);

                    SceneAuthoredActorScopePolicy.ValidateOrThrow(
                        sceneAuthoredActor.SceneActorScope,
                        actor.ActorId,
                        sourceScene.name);

                    if (sceneAuthoredActor.SceneActorScope != expectedScope)
                    {
                        continue;
                    }

                    string actorId = actor.ActorId.TrimToEmpty();
                    string actorType = nameof(Actor);
                    var actorInstanceRuntimeId = ActorInstanceRuntimeId.FromScopedRuntimeActorIdentity(
                        identity,
                        actorId,
                        sceneAuthoredActor.SceneActorScope,
                        sceneAuthoredActor.SceneActorScope.ToString());
                    if (!actorInstanceRuntimeId.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"ActorSceneDiscovery generated invalid actor instance runtime identity actorId='{actorId}' actorType='{actorType}' actorScope='{sceneAuthoredActor.SceneActorScope}'.");
                    }

                    actor.SetRuntimeActorInstanceId(actorInstanceRuntimeId);

                    SceneAuthoredActorIdentityRecord resolvedIdentity = new(
                        identity,
                        actorInstanceRuntimeId,
                        actorId,
                        actor.ActorRoleMetadata,
                        sceneAuthoredActor.SceneActorScope,
                        sceneAuthoredActor.SceneActorParticipationPolicy,
                        sceneAuthoredActor.ResolveExplicitParticipationActivityIdsOrFail(context),
                        originSource,
                        sourceScene.name,
                        actorType);
                    registry.RegisterDiscovered(resolvedIdentity, actor, actor.gameObject);
                    discovered.Add(new ActorSceneDiscoveredRecord(resolvedIdentity));
                }
            }
        }
    }
}
