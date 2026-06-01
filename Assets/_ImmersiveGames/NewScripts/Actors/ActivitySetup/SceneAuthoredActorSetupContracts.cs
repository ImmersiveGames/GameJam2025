using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
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
            ActorInstanceId actorInstanceId,
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
            ActorInstanceId = actorInstanceId;
            ActorId = Normalize(actorId);
            ActorRole = actorRole;
            ActorScope = actorScope;
            ParticipationPolicy = participationPolicy;
            ExplicitActivityIds = explicitActivityIds ?? Array.Empty<string>();
            OriginSourceKind = originSourceKind;
            OriginSceneName = Normalize(originSceneName);
            ActorType = Normalize(actorType);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceId ActorInstanceId { get; }
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
            ActorInstanceId.IsValid &&
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
                if (string.IsNullOrWhiteSpace(Normalize(ExplicitActivityIds[index])))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SceneAuthoredActorRuntimeEntry
    {
        public SceneAuthoredActorRuntimeEntry(
            SceneAuthoredActorIdentityRecord actorIdentity,
            Actor actor,
            GameObject actorInstance,
            ActorPresentationRuntimeHandle presentationHandle)
        {
            ActorIdentity = actorIdentity;
            Actor = actor;
            ActorInstance = actorInstance;
            PresentationHandle = presentationHandle;
        }

        public SceneAuthoredActorIdentityRecord ActorIdentity { get; }
        public Actor Actor { get; }
        public GameObject ActorInstance { get; }
        public ActorPresentationRuntimeHandle PresentationHandle { get; }
        public bool HasPresentationHandle => PresentationHandle.IsValid;
        public bool IsValid => ActorIdentity.IsValid && Actor != null && ActorInstance != null;
    }
}
