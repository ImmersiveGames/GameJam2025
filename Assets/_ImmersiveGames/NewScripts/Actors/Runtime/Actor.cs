using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private ActorInstanceId runtimeActorInstanceId;
        [SerializeField] private string actorId = string.Empty;
        [SerializeField] private ActorScope actorScope = ActorScope.Unknown;
        [SerializeField] private ActorParticipationRecord.ActorParticipationPolicy participationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
        [SerializeField] private ActorCapabilitySurface capabilitySurface;

        public virtual string ActorId => ActorIdValue.ToString();
        public ActorId ActorIdValue => new(Normalize(actorId));
        public abstract ActorRole ActorRoleMetadata { get; }
        public virtual ActorScope ActorScopeMetadata => actorScope;
        public virtual ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => participationPolicy;
        public virtual ActorCapabilitySurface CapabilitySurface
        {
            get
            {
                EnsureCapabilitySurfaceResolved();
                return capabilitySurface;
            }
        }
        public virtual ActorInstanceId RuntimeActorInstanceId => runtimeActorInstanceId;

        public virtual ActorDefinitionRef ActorDefinitionRef => default;

        public void SetRuntimeActorInstanceId(ActorInstanceId actorInstanceId)
        {
            runtimeActorInstanceId = actorInstanceId.IsValid ? actorInstanceId : default;
        }

        protected void SetActorIdValue(ActorId newActorId, string source)
        {
            if (!newActorId.IsValid)
            {
                string origin = ResolveOrigin(source, nameof(Actor), name);
                throw new InvalidOperationException($"{origin} cannot bind an invalid ActorId. actor='{name}'.");
            }

            actorId = Normalize(newActorId.Value);
        }

        public abstract void ValidateLocalConfigurationOrThrow(string source);

        protected void EnsureCapabilitySurfaceResolved()
        {
            if (capabilitySurface != null)
            {
                return;
            }

            capabilitySurface = GetComponent<ActorCapabilitySurface>();
            if (capabilitySurface == null)
            {
                capabilitySurface = GetComponentInChildren<ActorCapabilitySurface>(includeInactive: true);
            }
        }

        protected virtual void OnValidate()
        {
            actorId = Normalize(actorId);
            EnsureCapabilitySurfaceResolved();
        }

        protected static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        protected static string ResolveOrigin(string source, string fallbackName, string objectName)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                return source.Trim();
            }

            return $"{fallbackName}:{objectName}";
        }
    }
}
