using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActorScanTarget
    {
        public ActorScanTarget(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorDefinitionRef actorDefinitionRef,
            string actorId,
            ActorKind actorKind,
            Actor runtimeActor,
            ActorCapabilitySurface capabilitySurface,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorSourceKind actorSourceKind,
            string participationPolicy,
            GameObject actorRoot,
            string sourceSceneName,
            string componentBasePath,
            string source)
        {
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorDefinitionRef = actorDefinitionRef;
            ActorId = Normalize(actorId);
            ActorKind = actorKind;
            RuntimeActor = runtimeActor;
            CapabilitySurface = capabilitySurface;
            ActorRole = actorRole;
            ActorScope = actorScope;
            ActorSourceKind = actorSourceKind;
            ParticipationPolicy = Normalize(participationPolicy);
            ActorRoot = actorRoot;
            SourceSceneName = Normalize(sourceSceneName);
            ComponentBasePath = Normalize(componentBasePath);
            Source = Normalize(source);
        }

        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorDefinitionRef ActorDefinitionRef { get; }
        public string ActorId { get; }
        public ActorKind ActorKind { get; }
        public Actor RuntimeActor { get; }
        public ActorCapabilitySurface CapabilitySurface { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorSourceKind ActorSourceKind { get; }
        public string ParticipationPolicy { get; }
        public GameObject ActorRoot { get; }
        public string SourceSceneName { get; }
        public string ComponentBasePath { get; }
        public string Source { get; }

        public bool IsValid =>
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            ActorSourceKind != ActorSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(ParticipationPolicy) &&
            ActorRoot != null;

        public static bool TryFromInstance(ActorInstanceRecord instance, string source, out ActorScanTarget target)
        {
            target = default;
            if (!instance.IsValid)
            {
                return false;
            }

            var runtimeActorInstanceId = instance.ActorInstanceRuntimeId;
            if (!runtimeActorInstanceId.IsValid)
            {
                return false;
            }

            target = new ActorScanTarget(
                runtimeActorInstanceId,
                instance.DefinitionRef,
                instance.ActorId,
                instance.Kind,
                instance.RuntimeActor,
                instance.CapabilitySurface,
                instance.Role,
                instance.Scope,
                instance.SourceKind,
                instance.ParticipationPolicy,
                instance.ActorRoot,
                instance.SourceSceneName,
                instance.ComponentBasePath,
                source);
            return target.IsValid;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
