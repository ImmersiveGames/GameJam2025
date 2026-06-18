using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public interface ISceneAuthoredActor
    {
        ActorScope SceneActorScope { get; }
        ActorParticipationRecord.ActorParticipationPolicy SceneActorParticipationPolicy { get; }
        IReadOnlyList<string> ResolveExplicitParticipationActivityIdsOrFail(string source);
        void ValidateSceneAuthoredConfigurationOrThrow(string source);
    }

    public readonly struct SceneAuthoredActorIdentityRecord
    {
        public SceneAuthoredActorIdentityRecord(
            SessionActivityIdentity identity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string actorId,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorParticipationRecord.ActorParticipationPolicy participationPolicy,
            IReadOnlyList<string> explicitActivityIds,
            ActorSourceKind originSourceKind,
            string originSceneName,
            string actorType)
        {
            Identity = identity;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorId = actorId.TrimToEmpty();
            ActorRole = actorRole;
            ActorScope = actorScope;
            ParticipationPolicy = participationPolicy;
            ExplicitActivityIds = explicitActivityIds ?? Array.Empty<string>();
            OriginSourceKind = originSourceKind;
            OriginSceneName = originSceneName.TrimToEmpty();
            ActorType = actorType.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string ActorId { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorParticipationRecord.ActorParticipationPolicy ParticipationPolicy { get; }
        public IReadOnlyList<string> ExplicitActivityIds { get; }
        public ActorSourceKind OriginSourceKind { get; }
        public string OriginSceneName { get; }
        public string ActorType { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            OriginSourceKind != ActorSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(OriginSceneName) &&
            !string.IsNullOrWhiteSpace(ActorType) &&
            HasValidExplicitActivities();

        private bool HasValidExplicitActivities()
        {
            if (ParticipationPolicy != ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (ExplicitActivityIds == null || ExplicitActivityIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < ExplicitActivityIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(ExplicitActivityIds[index].TrimToEmpty()))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct SceneAuthoredActorRuntimeEntry
    {
        public SceneAuthoredActorRuntimeEntry(
            SceneAuthoredActorIdentityRecord actorIdentity,
            Actor actor,
            GameObject actorInstance)
        {
            ActorIdentity = actorIdentity;
            Actor = actor;
            ActorInstance = actorInstance;
        }

        public SceneAuthoredActorIdentityRecord ActorIdentity { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => ActorIdentity.ActorInstanceRuntimeId;
        public Actor Actor { get; }
        public GameObject ActorInstance { get; }
        public bool IsValid => ActorIdentity.IsValid && ActorInstanceRuntimeId.IsValid && Actor != null && ActorInstance != null;
    }
}
