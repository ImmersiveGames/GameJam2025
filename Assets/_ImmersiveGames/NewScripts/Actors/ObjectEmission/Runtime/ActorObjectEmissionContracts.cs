using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Authoring;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    public enum ActorObjectEmissionStatus
    {
        Unknown = 0,
        Accepted = 1,
        RejectedUnsupportedCommand = 2,
        RejectedInactive = 3
    }

    public readonly struct ActorObjectEmissionCommand
    {
        public ActorObjectEmissionCommand(
            ActorCommandEnvelope command,
            ActorObjectEmissionProfile profile)
        {
            Command = command;
            Profile = profile;
        }

        public ActorCommandEnvelope Command { get; }
        public ActorObjectEmissionProfile Profile { get; }
        public ActorId ActorId => Command.ActorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => Command.ActorInstanceRuntimeId;
        public ActorCommandId CommandId => Command.CommandId;
        public ActorCommandSourceKind SourceKind => Command.SourceKind;
        public string SourceId => Command.SourceId;
        public ActorCommandValue Value => Command.Value;
        public string Source => Command.Source;
        public string Reason => Command.Reason;
        public string ProfileId => Profile?.ProfileId ?? string.Empty;
        public PoolDefinitionAsset PoolDefinition => Profile?.PoolDefinition;
        public ActorObjectEmissionOriginPolicy EmissionOriginPolicy => Profile?.EmissionOriginPolicy ?? ActorObjectEmissionOriginPolicy.Unknown;
        public float ObjectSpeed => Profile?.ObjectSpeed ?? 0f;
        public float ObjectLifetime => Profile?.ObjectLifetime ?? 0f;
        public LayerMask ObjectLayerMask => Profile != null ? Profile.ObjectLayerMask : default;

        public bool IsValid =>
            Command.IsValid &&
            Command.CommandId.ValueKind == ActorCommandValueKind.FirePrimary &&
            Command.CommandId.TriggerKind == ActorCommandTriggerKind.Pressed &&
            Profile != null &&
            Profile.IsValid;
    }

    public readonly struct ActorObjectEmissionResult
    {
        public ActorObjectEmissionResult(ActorObjectEmissionStatus status, string reason)
        {
            Status = status;
            Reason = Normalize(reason);
        }

        public ActorObjectEmissionStatus Status { get; }
        public string Reason { get; }

        public bool IsValid => Status != ActorObjectEmissionStatus.Unknown;
        public bool IsAccepted => Status == ActorObjectEmissionStatus.Accepted;

        public static ActorObjectEmissionResult Accepted(string reason = "")
        {
            return new ActorObjectEmissionResult(ActorObjectEmissionStatus.Accepted, reason);
        }

        public static ActorObjectEmissionResult RejectedUnsupportedCommand(string reason)
        {
            return new ActorObjectEmissionResult(ActorObjectEmissionStatus.RejectedUnsupportedCommand, reason);
        }

        public static ActorObjectEmissionResult RejectedInactive(string reason)
        {
            return new ActorObjectEmissionResult(ActorObjectEmissionStatus.RejectedInactive, reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActorObjectEmitterEndpoint : IActorCommandSink
    {
        ActorObjectEmissionResult Fire(in ActorObjectEmissionCommand command);
    }
}
