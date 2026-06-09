using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private ActorInstanceRuntimeId _runtimeActorInstanceId;
        [SerializeField] private ActorCapabilitySurface capabilitySurface;

        public virtual string ActorId => ActorIdValue.ToString();
        public abstract ActorId ActorIdValue { get; }
        public abstract ActorRole ActorRoleMetadata { get; }
        public abstract ActorScope ActorScopeMetadata { get; }
        public abstract ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy { get; }
        public virtual ActorCapabilitySurface CapabilitySurface
        {
            get
            {
                EnsureCapabilitySurfaceResolved();
                return capabilitySurface;
            }
        }
        public virtual ActorInstanceRuntimeId RuntimeActorInstanceId => _runtimeActorInstanceId;

        public virtual ActorDefinitionRef ActorDefinitionRef => default;

        public void SetRuntimeActorInstanceId(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            _runtimeActorInstanceId = actorInstanceRuntimeId.IsValid ? actorInstanceRuntimeId : default;
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
