using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private ActorInstanceId _runtimeActorInstanceId;
        [SerializeField] private ActorRole baseActorRoleMetadata = ActorRole.Unknown;
        [SerializeField] private ActorScope baseActorScopeMetadata = ActorScope.Unknown;
        [SerializeField] private ActorCapabilitySurface capabilitySurface;

        public abstract string ActorId { get; }
        public virtual ActorRole ActorRoleMetadata => baseActorRoleMetadata;
        public virtual ActorScope ActorScopeMetadata => baseActorScopeMetadata;
        public virtual ActorCapabilitySurface CapabilitySurface
        {
            get
            {
                EnsureCapabilitySurfaceResolved();
                return capabilitySurface;
            }
        }
        public virtual ActorInstanceId RuntimeActorInstanceId => _runtimeActorInstanceId;

        public virtual ActorDefinitionRef ActorDefinitionRef => default;

        public void SetRuntimeActorInstanceId(ActorInstanceId actorInstanceId)
        {
            _runtimeActorInstanceId = actorInstanceId.IsValid ? actorInstanceId : default;
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
