using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public enum ActorProjectileFireStatus
    {
        Unknown = 0,
        Accepted = 1,
        RejectedUnsupportedCommand = 2,
        RejectedInactive = 3
    }

    public readonly struct ActorProjectileFireCommand
    {
        public ActorProjectileFireCommand(ActorCommandEnvelope command)
        {
            Command = command;
        }

        public ActorCommandEnvelope Command { get; }
        public ActorId ActorId => Command.ActorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => Command.ActorInstanceRuntimeId;
        public ActorCommandId CommandId => Command.CommandId;
        public ActorCommandSourceKind SourceKind => Command.SourceKind;
        public string SourceId => Command.SourceId;
        public ActorCommandValue Value => Command.Value;
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Command.IsValid &&
            Command.CommandId.ValueKind == ActorCommandValueKind.FirePrimary &&
            Command.CommandId.TriggerKind == ActorCommandTriggerKind.Pressed;
    }

    public readonly struct ActorProjectileFireResult
    {
        public ActorProjectileFireResult(ActorProjectileFireStatus status, string reason)
        {
            Status = status;
            Reason = Normalize(reason);
        }

        public ActorProjectileFireStatus Status { get; }
        public string Reason { get; }

        public bool IsValid => Status != ActorProjectileFireStatus.Unknown;
        public bool IsAccepted => Status == ActorProjectileFireStatus.Accepted;

        public static ActorProjectileFireResult Accepted(string reason = "")
        {
            return new ActorProjectileFireResult(ActorProjectileFireStatus.Accepted, reason);
        }

        public static ActorProjectileFireResult RejectedUnsupportedCommand(string reason)
        {
            return new ActorProjectileFireResult(ActorProjectileFireStatus.RejectedUnsupportedCommand, reason);
        }

        public static ActorProjectileFireResult RejectedInactive(string reason)
        {
            return new ActorProjectileFireResult(ActorProjectileFireStatus.RejectedInactive, reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActorProjectileEmitterEndpoint : IActorCommandSink
    {
        ActorProjectileFireResult Fire(in ActorProjectileFireCommand command);
    }
}
