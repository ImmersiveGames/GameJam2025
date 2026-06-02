using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerActorMaterializationAdapter : IPlayerActorMaterializationAdapter
    {
        private const string RuntimeRootName = "__ActivityPlayerActorsRuntimeRoot";
        private const string SessionRuntimeRootName = "__SessionActorsRuntimeRoot";

        public IReadOnlyList<PlayerActorMaterializationRecord> Execute(PlayerActorMaterializationCommand command, SessionActivityIdentity activeIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerActorMaterializationCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active activity identity is invalid for player actor materialization.");
            }

            if (!command.PipelineIdentity.Equals(activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_command: command identity does not match active identity.");
            }

            List<PlayerActorMaterializationRecord> records = new(command.Entries.Count);

            for (int index = 0; index < command.Entries.Count; index++)
            {
                PlayerActorEntryPlan plan = command.Entries[index];
                if (!plan.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorEntryPlan at index '{index}' is invalid.");
                }

                if (!plan.ActorIdentity.Identity.Equals(activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_actor_plan: plan identity does not match active identity.");
                }

                PlayerActor actor = plan.Prefab.GetComponent<PlayerActor>();
                if (actor == null)
                {
                    throw new InvalidOperationException($"PlayerActor prefab missing PlayerActor component. prefab='{plan.Prefab.name}' playerSlotId='{plan.ActorIdentity.PlayerSlotId}'.");
                }

                ActorScope actorScope = plan.ActorIdentity.ParticipantBinding.ActorScope;
                if (actorScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"Player actor materialization requires ActorScope from ActivityParticipantBinding. participantId='{plan.ActorIdentity.ParticipantId}' playerSlotId='{plan.ActorIdentity.PlayerSlotId}'.");
                }

                SceneContext scene = ResolveActiveSceneOrFail();
                Transform root = EnsureRuntimeRoot(scene, activeIdentity, actorScope);
                GameObject instance = UnityEngine.Object.Instantiate(plan.Prefab, root);
                instance.name = $"PlayerActor::{plan.ActorIdentity.PlayerSlotId}::{plan.ActorIdentity.PlayerActorId}";
                instance.transform.localPosition = plan.LocalPosition;
                instance.transform.localRotation = Quaternion.Euler(plan.LocalEulerAngles);

                actor = instance.GetComponent<PlayerActor>();
                actor.BindRuntimeMetadata(
                    plan.ActorIdentity.ActorId,
                    actorScope,
                    ActorParticipationRecord.ActorParticipationPolicy.AllActivitiesInRoute,
                    nameof(PlayerActorMaterializationAdapter));
                ActorInstanceId runtimeActorInstanceId = ActorInstanceId.FromScopedRuntimeActorIdentity(
                    activeIdentity,
                    actor.ActorId,
                    actor.ActorScopeMetadata,
                    actor.ActorScopeMetadata.ToString());
                if (!runtimeActorInstanceId.IsValid)
                {
                    throw new InvalidOperationException(
                        $"player_actor_runtime_identity_missing_after_materialization: playerSlotId='{plan.ActorIdentity.PlayerSlotId}' playerActorId='{plan.ActorIdentity.PlayerActorId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                }

                actor.SetRuntimeActorInstanceId(runtimeActorInstanceId);

                PlayerActorIdentity identity = instance.GetComponent<PlayerActorIdentity>();
                if (identity == null)
                {
                    identity = instance.AddComponent<PlayerActorIdentity>();
                }

                identity.Bind(activeIdentity, plan.ActorIdentity);

                PlayerActorParticipationState participation = instance.GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = instance.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkActiveInActivity(activeIdentity);

                MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
                bool hasResetEndpoint = false;
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    if (behaviours[behaviourIndex] is IActorResetEndpoint)
                    {
                        hasResetEndpoint = true;
                        break;
                    }
                }

                if (!hasResetEndpoint)
                {
                    throw new InvalidOperationException(
                        $"actor_reset_endpoint_missing_on_actor_prefab: prefab='{plan.Prefab.name}' playerSlotId='{plan.ActorIdentity.PlayerSlotId}' playerActorId='{plan.ActorIdentity.PlayerActorId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                }

                records.Add(new PlayerActorMaterializationRecord(new PlayerActorRuntimeHandle(plan.ActorIdentity, instance, actor)));
            }

            return records;
        }

        private static SceneContext ResolveActiveSceneOrFail()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Active scene is invalid or not loaded for player actor materialization.");
            }

            return new SceneContext(scene);
        }

        private static Transform EnsureRuntimeRoot(SceneContext context, SessionActivityIdentity identity, ActorScope actorScope)
        {
            if (actorScope == ActorScope.Unknown)
            {
                throw new InvalidOperationException("Player actor materialization requires explicit ActorScope.");
            }

            if (actorScope == ActorScope.SessionScoped)
            {
                return EnsureSessionRuntimeRoot(identity);
            }

            string expected = $"{RuntimeRootName}::{identity.SessionId}::{identity.ActivityId}::{identity.EntrySequence}";
            GameObject[] roots = context.Scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null && string.Equals(root.name, expected, StringComparison.Ordinal))
                {
                    return root.transform;
                }
            }

            GameObject created = new(expected);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(created, context.Scene);
            return created.transform;
        }

        private static Transform EnsureSessionRuntimeRoot(SessionActivityIdentity identity)
        {
            string expected = $"{SessionRuntimeRootName}::{identity.SessionId}";
            GameObject existing = GameObject.Find(expected);
            if (existing != null)
            {
                return existing.transform;
            }

            GameObject created = new(expected);
            UnityEngine.Object.DontDestroyOnLoad(created);
            return created.transform;
        }

        private readonly struct SceneContext
        {
            public SceneContext(UnityEngine.SceneManagement.Scene scene)
            {
                Scene = scene;
            }

            public UnityEngine.SceneManagement.Scene Scene { get; }
        }
    }
}
