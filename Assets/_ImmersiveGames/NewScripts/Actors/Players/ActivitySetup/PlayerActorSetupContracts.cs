using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public readonly struct PlayerActorIdentityRecord
    {
        public PlayerActorIdentityRecord(
            SessionActivityIdentity identity,
            ActivityParticipantBinding participantBinding,
            PlayerActorId playerActorId)
        {
            Identity = identity;
            ParticipantBinding = participantBinding;
            PlayerActorId = playerActorId;
        }

        public SessionActivityIdentity Identity { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public ActivityParticipantRequirementId RequirementId => ParticipantBinding.RequirementId;
        public SessionParticipantId ParticipantId => ParticipantBinding.ParticipantId;
        public PlayerSlotId PlayerSlotId => ParticipantBinding.PlayerSlotId;
        public PlayerSelectionId PlayerSelectionId => ParticipantBinding.PlayerSelectionId;
        public ActorDefinitionId ActorDefinitionId => ParticipantBinding.ActorDefinitionId;
        public ActorId ActorId => ParticipantBinding.ActorId;
        public PlayerActorId PlayerActorId { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ParticipantBinding.IsValid &&
            PlayerActorId.IsValid;

        public static PlayerActorIdentityRecord Create(
            SessionActivityIdentity identity,
            ActivityParticipantBinding participantBinding)
        {
            if (!identity.IsValid || !participantBinding.IsValid)
            {
                return default;
            }

            PlayerActorId playerActorId = BuildPlayerActorId(identity, participantBinding.ActorId);
            if (!playerActorId.IsValid)
            {
                return default;
            }

            return new PlayerActorIdentityRecord(identity, participantBinding, playerActorId);
        }

        private static PlayerActorId BuildPlayerActorId(SessionActivityIdentity identity, ActorId actorId)
        {
            if (!identity.IsValid || !actorId.IsValid)
            {
                return default;
            }

            return new PlayerActorId($"{identity.SessionId}|{actorId}");
        }
    }

    public readonly struct PlayerActorRuntimeHandle
    {
        public PlayerActorRuntimeHandle(
            PlayerActorIdentityRecord actorIdentity,
            GameObject instance,
            IActor actor)
        {
            ActorIdentity = actorIdentity;
            Instance = instance;
            Actor = actor;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public GameObject Instance { get; }
        public IActor Actor { get; }

        public ActivityParticipantBinding ParticipantBinding => ActorIdentity.ParticipantBinding;
        public ActivityParticipantRequirementId RequirementId => ActorIdentity.RequirementId;
        public SessionParticipantId ParticipantId => ActorIdentity.ParticipantId;
        public PlayerSlotId PlayerSlotId => ActorIdentity.PlayerSlotId;
        public PlayerSelectionId PlayerSelectionId => ActorIdentity.PlayerSelectionId;
        public ActorDefinitionId ActorDefinitionId => ActorIdentity.ActorDefinitionId;
        public ActorId ActorId => ActorIdentity.ActorId;
        public PlayerActorId PlayerActorId => ActorIdentity.PlayerActorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId
        {
            get
            {
                if (Actor == null || !Actor.RuntimeActorInstanceId.IsValid)
                {
                    return default;
                }

                return Actor.RuntimeActorInstanceId;
            }
        }
        public ActorCapabilitySurface CapabilitySurface => Actor?.CapabilitySurface;

        public bool IsValid => ActorIdentity.IsValid && Instance != null && Actor != null;
    }

    public readonly struct PlayerActorEntryPlan
    {
        public PlayerActorEntryPlan(
            PlayerActorIdentityRecord actorIdentity,
            GameObject prefab,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            ActorIdentity = actorIdentity;
            Prefab = prefab;
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public GameObject Prefab { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public bool IsValid => ActorIdentity.IsValid && Prefab != null;
    }

    public readonly struct PlayerActorActivityParticipationPlan
    {
        public PlayerActorActivityParticipationPlan(PlayerActorIdentityRecord actorIdentity)
        {
            ActorIdentity = actorIdentity;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool IsValid => ActorIdentity.IsValid;
    }

    public readonly struct PlayerActorMaterializationCommand
    {
        public PlayerActorMaterializationCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorEntryPlan> entries,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Entries = entries ?? Array.Empty<PlayerActorEntryPlan>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorEntryPlan> Entries { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Entries != null;
}

    public readonly struct PlayerActorMaterializationRecord
    {
        public PlayerActorMaterializationRecord(PlayerActorRuntimeHandle runtimeHandle)
        {
            RuntimeHandle = runtimeHandle;
        }

        public PlayerActorRuntimeHandle RuntimeHandle { get; }
        public PlayerActorIdentityRecord ActorIdentity => RuntimeHandle.ActorIdentity;
        public GameObject Instance => RuntimeHandle.Instance;
        public IActor Actor => RuntimeHandle.Actor;
        public bool IsValid => RuntimeHandle.IsValid;
    }

    public interface IPlayerActorMaterializationAdapter
    {
        IReadOnlyList<PlayerActorMaterializationRecord> Execute(PlayerActorMaterializationCommand command, SessionActivityIdentity activeIdentity);
    }

    public readonly struct PlayerActorParticipationExitCommand
    {
        public PlayerActorParticipationExitCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null;
}

    public readonly struct PlayerActorParticipationExitRecord
    {
        public PlayerActorParticipationExitRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool exited)
        {
            ActorIdentity = actorIdentity;
            Exited = exited;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Exited { get; }
        public bool IsValid => ActorIdentity.IsValid && Exited;
    }

    public interface IPlayerActorParticipationAdapter
    {
        IReadOnlyList<PlayerActorParticipationExitRecord> Execute(
            PlayerActorParticipationExitCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);

        IReadOnlyList<PlayerActorParticipationEnterRecord> Execute(
            PlayerActorParticipationEnterCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct PlayerActorParticipationEnterCommand
    {
        public PlayerActorParticipationEnterCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null;
}

    public readonly struct PlayerActorParticipationEnterRecord
    {
        public PlayerActorParticipationEnterRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool entered)
        {
            ActorIdentity = actorIdentity;
            Entered = entered;
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Entered { get; }
        public bool IsValid => ActorIdentity.IsValid && Entered;
    }

    public readonly struct PlayerInputBindingRequirement
    {
        public PlayerInputBindingRequirement(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantBinding participantBinding,
            bool required,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = requirementId.TrimToEmpty();
            ParticipantBinding = participantBinding;
            Required = required;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public SessionParticipantId ParticipantId => ParticipantBinding.ParticipantId;
        public PlayerSlotId PlayerSlotId => ParticipantBinding.PlayerSlotId;
        public PlayerSelectionId PlayerSelectionId => ParticipantBinding.PlayerSelectionId;
        public ActorDefinitionId ActorDefinitionId => ParticipantBinding.ActorDefinitionId;
        public ActorId ActorId => ParticipantBinding.ActorId;
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct PlayerInputBindingCommand
    {
        public PlayerInputBindingCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerInputBindingRequirement> requirements,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Requirements = requirements ?? Array.Empty<PlayerInputBindingRequirement>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerInputBindingRequirement> Requirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Requirements != null && !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct PlayerInputBindingRecord
    {
        public PlayerInputBindingRecord(
            PlayerInputBindingRequirement requirement,
            PlayerActorIdentityRecord actorIdentity,
            bool bound,
            string observedInputId)
        {
            Requirement = requirement;
            ActorIdentity = actorIdentity;
            Bound = bound;
            ObservedInputId = observedInputId.TrimToEmpty();
        }

        public PlayerInputBindingRequirement Requirement { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Bound { get; }
        public string ObservedInputId { get; }
        public bool IsValid => Requirement.IsValid && ActorIdentity.IsValid && Bound && !string.IsNullOrWhiteSpace(ObservedInputId);
}

    public interface IPlayerInputBindingAdapter
    {
        IReadOnlyList<PlayerInputBindingRecord> Execute(
            PlayerInputBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct ActorCommandBindingReference
    {
        public ActorCommandBindingReference(
            string requirementId,
            ActivityParticipantRequirementKind participantKind,
            ActivityParticipantBinding participantBinding,
            bool required)
        {
            RequirementId = requirementId.TrimToEmpty();
            ParticipantKind = participantKind;
            ParticipantBinding = participantBinding;
            Required = required;
        }

        public string RequirementId { get; }
        public ActivityParticipantRequirementKind ParticipantKind { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public bool Required { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
            ParticipantBinding.IsValid;
}

    public enum ActorCommandBindingState
    {
        Unknown = 0,
        Declared = 1,
        Prepared = 2,
        SinkBound = 3,
        Executable = 4,
        SkippedOptional = 5,
        MissingRequired = 6
    }

    public readonly struct ActorCommandBindingCommand
    {
        public ActorCommandBindingCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<ActorCommandBindingReference> bindings,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Bindings = bindings ?? Array.Empty<ActorCommandBindingReference>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<ActorCommandBindingReference> Bindings { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Bindings != null && !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct ActorCommandBindingRecord
    {
        public ActorCommandBindingRecord(
            ActorCommandBindingReference requirement,
            PlayerActorIdentityRecord actorIdentity,
            ActorCommandBindingState state,
            string observedEndpoint)
        {
            Requirement = requirement;
            ActorIdentity = actorIdentity;
            State = state;
            ObservedEndpoint = observedEndpoint.TrimToEmpty();
        }

        public ActorCommandBindingReference Requirement { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public ActorCommandBindingState State { get; }
        public string ObservedEndpoint { get; }
        public bool Bound => State == ActorCommandBindingState.SinkBound ||
            State == ActorCommandBindingState.Executable;
        public bool Skipped => State == ActorCommandBindingState.SkippedOptional;
        public bool IsPrepared => State == ActorCommandBindingState.Prepared ||
            State == ActorCommandBindingState.SinkBound ||
            State == ActorCommandBindingState.Executable;
        public bool IsSinkBound => State == ActorCommandBindingState.SinkBound ||
            State == ActorCommandBindingState.Executable;
        public bool IsExecutable => State == ActorCommandBindingState.Executable;
        public bool IsValid => Requirement.IsValid && ActorIdentity.IsValid && !string.IsNullOrWhiteSpace(ObservedEndpoint) && State != ActorCommandBindingState.Unknown;
}

    public interface IActorCommandBindingAdapter
    {
        IReadOnlyList<ActorCommandBindingRecord> Execute(
            ActorCommandBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct ActorCommandBindingResult
    {
        public ActorCommandBindingResult(
            SessionActivityIdentity identity,
            int totalRequirements,
            int requiredRequirements,
            int requiredBoundCount,
            int totalBoundCount,
            int skippedCount,
            bool skipped,
            string reason)
        {
            Identity = identity;
            TotalRequirements = totalRequirements < 0 ? 0 : totalRequirements;
            RequiredRequirements = requiredRequirements < 0 ? 0 : requiredRequirements;
            RequiredBoundCount = requiredBoundCount < 0 ? 0 : requiredBoundCount;
            TotalBoundCount = totalBoundCount < 0 ? 0 : totalBoundCount;
            SkippedCount = skippedCount < 0 ? 0 : skippedCount;
            Skipped = skipped;
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public int TotalRequirements { get; }
        public int RequiredRequirements { get; }
        public int RequiredBoundCount { get; }
        public int TotalBoundCount { get; }
        public int SkippedCount { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool Completed => Identity.IsValid;
        public bool IsValid =>
            Completed &&
            RequiredRequirements >= 0 &&
            RequiredBoundCount >= 0 &&
            RequiredBoundCount <= RequiredRequirements &&
            TotalBoundCount >= 0 &&
            SkippedCount >= 0;
}

    public readonly struct MovementBindingRequirement
    {
        public MovementBindingRequirement(
            SessionActivityIdentity identity,
            string requirementId,
            ActivityParticipantBinding participantBinding,
            bool required,
            string source,
            string reason)
        {
            Identity = identity;
            RequirementId = requirementId.TrimToEmpty();
            ParticipantBinding = participantBinding;
            Required = required;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string RequirementId { get; }
        public ActivityParticipantBinding ParticipantBinding { get; }
        public SessionParticipantId ParticipantId => ParticipantBinding.ParticipantId;
        public PlayerSlotId PlayerSlotId => ParticipantBinding.PlayerSlotId;
        public PlayerSelectionId PlayerSelectionId => ParticipantBinding.PlayerSelectionId;
        public ActorDefinitionId ActorDefinitionId => ParticipantBinding.ActorDefinitionId;
        public ActorId ActorId => ParticipantBinding.ActorId;
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(RequirementId) &&
            ParticipantBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct MovementBindingCommand
    {
        public MovementBindingCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<MovementBindingRequirement> requirements,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Requirements = requirements ?? Array.Empty<MovementBindingRequirement>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<MovementBindingRequirement> Requirements { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => PipelineIdentity.IsValid && Requirements != null && !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct MovementBindingRecord
    {
        public MovementBindingRecord(
            MovementBindingRequirement requirement,
            PlayerActorIdentityRecord actorIdentity,
            bool bound,
            string observedEndpoint)
        {
            Requirement = requirement;
            ActorIdentity = actorIdentity;
            Bound = bound;
            ObservedEndpoint = observedEndpoint.TrimToEmpty();
        }

        public MovementBindingRequirement Requirement { get; }
        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Bound { get; }
        public string ObservedEndpoint { get; }
        public bool IsValid => Requirement.IsValid && ActorIdentity.IsValid && Bound && !string.IsNullOrWhiteSpace(ObservedEndpoint);
}

    public interface IMovementBindingAdapter
    {
        IReadOnlyList<MovementBindingRecord> Execute(
            MovementBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }

    public readonly struct MovementControlCommand
    {
        public MovementControlCommand(
            SessionActivityIdentity pipelineIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> actors,
            bool enable,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity;
            Actors = actors ?? Array.Empty<PlayerActorIdentityRecord>();
            Enable = enable;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity PipelineIdentity { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> Actors { get; }
        public bool Enable { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => PipelineIdentity.IsValid && Actors != null && !string.IsNullOrWhiteSpace(Source);
}

    public readonly struct MovementControlRecord
    {
        public MovementControlRecord(
            PlayerActorIdentityRecord actorIdentity,
            bool enabled,
            string observedEndpoint)
        {
            ActorIdentity = actorIdentity;
            Enabled = enabled;
            ObservedEndpoint = observedEndpoint.TrimToEmpty();
        }

        public PlayerActorIdentityRecord ActorIdentity { get; }
        public bool Enabled { get; }
        public string ObservedEndpoint { get; }
        public bool IsValid => ActorIdentity.IsValid && !string.IsNullOrWhiteSpace(ObservedEndpoint);
}

    public interface IPlayerMovementControlAdapter
    {
        IReadOnlyList<MovementControlRecord> Execute(
            MovementControlCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry);
    }
}
