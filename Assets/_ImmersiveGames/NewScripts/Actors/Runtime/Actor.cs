using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        [SerializeField] private string runtimeActorInstanceId = string.Empty;
        [SerializeField] private string actorDefinitionId = string.Empty;
        [SerializeField] private string actorDefinitionAssetName = string.Empty;
        [SerializeField] private string actorDefinitionAssetPath = string.Empty;
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
        public virtual ActorInstanceId RuntimeActorInstanceId => new(Normalize(runtimeActorInstanceId));

        public virtual ActorDefinitionRef ActorDefinitionRef =>
            new(
                new ActorDefinitionId(Normalize(actorDefinitionId)),
                Normalize(actorDefinitionAssetName),
                Normalize(actorDefinitionAssetPath));

        public void SetRuntimeActorInstanceId(ActorInstanceId actorInstanceId)
        {
            runtimeActorInstanceId = actorInstanceId.IsValid ? actorInstanceId.Value : string.Empty;
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
            runtimeActorInstanceId = Normalize(runtimeActorInstanceId);
            actorDefinitionId = Normalize(actorDefinitionId);
            actorDefinitionAssetName = Normalize(actorDefinitionAssetName);
            actorDefinitionAssetPath = Normalize(actorDefinitionAssetPath);

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
