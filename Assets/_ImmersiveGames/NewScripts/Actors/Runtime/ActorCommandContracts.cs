using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public enum ActorCommandKind
    {
        Unknown = 0,
        Move = 1,
        FirePrimary = 2
    }

    public enum ActorCommandValueKind
    {
        Unknown = 0,
        Vector2 = 1,
        Button = 2,
        Axis = 3,
        Trigger = 4
    }

    public enum ActorCommandTriggerKind
    {
        Unknown = 0,
        Continuous = 1,
        ValueChanged = 2,
        Pressed = 3,
        Released = 4
    }

    [Serializable]
    public readonly struct ActorCommandBindingId
    {
        public ActorCommandBindingId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value;
        }
}

    [Serializable]
    public readonly struct ActorCommandSourceIdentity
    {
        public ActorCommandSourceIdentity(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value;
        }
}

    [Serializable]
    public sealed class ActorCommandInputBinding
    {
        [SerializeField, FormerlySerializedAs("BindingId")]
        private string bindingId = string.Empty;

        [SerializeField, FormerlySerializedAs("Enabled")]
        private bool enabled = true;

        [SerializeField, FormerlySerializedAs("Required")]
        private bool required = false;

        [SerializeField, FormerlySerializedAs("CommandKind")]
        private ActorCommandKind commandKind = ActorCommandKind.Move;

        [SerializeField, FormerlySerializedAs("ValueKind")]
        private ActorCommandValueKind valueKind = ActorCommandValueKind.Vector2;

        [SerializeField, FormerlySerializedAs("TriggerKind")]
        private ActorCommandTriggerKind triggerKind = ActorCommandTriggerKind.Continuous;

        [SerializeField, FormerlySerializedAs("ActionReference")]
        private InputActionReference actionReference;

        [SerializeField, HideInInspector, FormerlySerializedAs("ActionMapName")]
        private string legacyActionMapName = string.Empty;

        [SerializeField, HideInInspector, FormerlySerializedAs("ActionName")]
        private string legacyActionName = string.Empty;

        public string BindingId => bindingId.TrimToEmpty();
        public bool Enabled => enabled;
        public bool Required => required;
        public ActorCommandKind CommandKind => commandKind;
        public ActorCommandValueKind ValueKind => valueKind;
        public ActorCommandTriggerKind TriggerKind => triggerKind;
        public InputActionReference ActionReference => actionReference;

        public bool IsConfigured =>
            HasBindingId &&
            CommandKind != ActorCommandKind.Unknown &&
            ValueKind != ActorCommandValueKind.Unknown &&
            TriggerKind != ActorCommandTriggerKind.Unknown &&
            IsCommandValueShapeValid;

        public bool IsActive => Enabled && IsConfigured;

        public bool HasBindingId => !string.IsNullOrWhiteSpace(BindingId);

        public bool HasExplicitActionReference =>
            ActionReference != null &&
            ActionReference.action != null;

        public bool HasLegacyActionLocator =>
            !string.IsNullOrWhiteSpace(legacyActionMapName) &&
            !string.IsNullOrWhiteSpace(legacyActionName);

        public string LegacyActionMapName => legacyActionMapName.TrimToEmpty();
        public string LegacyActionName => legacyActionName.TrimToEmpty();

        public bool IsCommandValueShapeValid => IsCommandValueCompatible(ResolveCommandId(), ValueKind, TriggerKind);

        public bool Matches(
            ActorCommandId commandId,
            ActorCommandTriggerKind triggerKind)
        {
            return IsActive &&
                TryResolveCommandId(out var declaredCommandId) &&
                declaredCommandId == commandId &&
                TriggerKind == triggerKind;
        }

        public bool TryResolveCommandId(out ActorCommandId commandId)
        {
            commandId = ResolveCommandId();
            return commandId.IsValid;
        }

        public ActorCommandId ResolveCommandIdOrFail()
        {
            if (!TryResolveCommandId(out var commandId))
            {
                throw new InvalidOperationException($"ActorCommandInputBinding requires a supported command kind for binding '{BindingId}'.");
            }

            return commandId;
        }

        public ActorCommandBindingId ResolveBindingIdOrFail()
        {
            if (!HasBindingId)
            {
                throw new InvalidOperationException("ActorCommandInputBinding requires a non-empty BindingId.");
            }

            return new ActorCommandBindingId(BindingId);
        }

        private ActorCommandId ResolveCommandId()
        {
            return CommandKind switch
            {
                ActorCommandKind.Move => ActorCommandId.Move,
                ActorCommandKind.FirePrimary => ActorCommandId.FirePrimary,
                _ => default
            };
        }
internal static bool IsCommandValueCompatible(
            ActorCommandId commandId,
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind)
        {
            if (!commandId.IsValid ||
                valueKind == ActorCommandValueKind.Unknown ||
                triggerKind == ActorCommandTriggerKind.Unknown)
            {
                return false;
            }

            if (commandId == ActorCommandId.Move)
            {
                return valueKind == ActorCommandValueKind.Vector2 &&
                    (triggerKind == ActorCommandTriggerKind.Continuous ||
                        triggerKind == ActorCommandTriggerKind.ValueChanged);
            }

            if (commandId == ActorCommandId.FirePrimary)
            {
                return valueKind == ActorCommandValueKind.Button &&
                    triggerKind == ActorCommandTriggerKind.Pressed;
            }

            return false;
        }
    }

    public readonly struct ActorCommandValue
    {
        private ActorCommandValue(
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind,
            Vector2 vector2Value,
            bool boolValue,
            float floatValue)
        {
            ValueKind = valueKind;
            TriggerKind = triggerKind;
            Vector2Value = vector2Value;
            BoolValue = boolValue;
            FloatValue = floatValue;
        }

        public ActorCommandValueKind ValueKind { get; }
        public ActorCommandTriggerKind TriggerKind { get; }
        public Vector2 Vector2Value { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }

        public bool IsValid =>
            ValueKind != ActorCommandValueKind.Unknown &&
            TriggerKind != ActorCommandTriggerKind.Unknown;

        public static ActorCommandValue CreateMove(Vector2 value, ActorCommandTriggerKind triggerKind = ActorCommandTriggerKind.Continuous)
        {
            return new ActorCommandValue(ActorCommandValueKind.Vector2, triggerKind, value, boolValue: false, floatValue: 0f);
        }

        public static ActorCommandValue CreateFirePrimary(bool pressed, ActorCommandTriggerKind triggerKind = ActorCommandTriggerKind.Pressed)
        {
            return new ActorCommandValue(ActorCommandValueKind.Button, triggerKind, Vector2.zero, pressed, floatValue: 0f);
        }

        public static ActorCommandValue CreateButton(
            ActorCommandTriggerKind triggerKind,
            bool pressed)
        {
            return new ActorCommandValue(ActorCommandValueKind.Button, triggerKind, Vector2.zero, pressed, floatValue: 0f);
        }
    }

    public readonly struct ActorCommandId : IEquatable<ActorCommandId>
    {
        public ActorCommandId(ActorCommandKind kind)
        {
            Kind = kind;
        }

        public ActorCommandKind Kind { get; }

        public bool IsValid => Kind != ActorCommandKind.Unknown;

        public static ActorCommandId Move => new(ActorCommandKind.Move);
        public static ActorCommandId FirePrimary => new(ActorCommandKind.FirePrimary);

        public bool Equals(ActorCommandId other) => Kind == other.Kind;
        public override bool Equals(object obj) => obj is ActorCommandId other && Equals(other);
        public override int GetHashCode() => Kind.GetHashCode();

        public static bool operator ==(ActorCommandId left, ActorCommandId right) => left.Equals(right);
        public static bool operator !=(ActorCommandId left, ActorCommandId right) => !left.Equals(right);

        public override string ToString()
        {
            return Kind.ToString();
        }
    }

    public readonly struct ActorCommandEnvelope
    {
        public ActorCommandEnvelope(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorCommandId commandId,
            ActorCommandBindingId bindingId,
            ActorCommandSourceIdentity sourceIdentity,
            int sequence,
            ActorCommandValue value,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            CommandId = commandId;
            BindingId = bindingId;
            SourceIdentity = sourceIdentity;
            Sequence = sequence < 0 ? 0 : sequence;
            Value = value;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCommandId CommandId { get; }
        public ActorCommandBindingId BindingId { get; }
        public ActorCommandSourceIdentity SourceIdentity { get; }
        public int Sequence { get; }
        public ActorCommandValue Value { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            CommandId.IsValid &&
            BindingId.IsValid &&
            SourceIdentity.IsValid &&
            Value.IsValid &&
            ActorCommandInputBinding.IsCommandValueCompatible(CommandId, Value.ValueKind, Value.TriggerKind) &&
            Sequence >= 0;
}

    public enum ActorCommandDispatchStatus
    {
        Unknown = 0,
        Accepted = 1,
        RejectedUnsupportedCommand = 2,
        RejectedInactive = 3
    }

    public readonly struct ActorCommandDispatchResult
    {
        public ActorCommandDispatchResult(
            ActorCommandDispatchStatus status,
            string reason)
        {
            Status = status;
            Reason = reason.TrimToEmpty();
        }

        public ActorCommandDispatchStatus Status { get; }
        public string Reason { get; }

        public bool IsValid => Status != ActorCommandDispatchStatus.Unknown;
        public bool IsAccepted => Status == ActorCommandDispatchStatus.Accepted;

        public static ActorCommandDispatchResult Accepted(string reason = "")
        {
            return new ActorCommandDispatchResult(ActorCommandDispatchStatus.Accepted, reason);
        }

        public static ActorCommandDispatchResult RejectedUnsupportedCommand(string reason)
        {
            return new ActorCommandDispatchResult(ActorCommandDispatchStatus.RejectedUnsupportedCommand, reason);
        }

        public static ActorCommandDispatchResult RejectedInactive(string reason)
        {
            return new ActorCommandDispatchResult(ActorCommandDispatchStatus.RejectedInactive, reason);
        }
}

    public interface IActorCommandSink
    {
        ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command);
    }

    public interface IActorCommandSourceHub
    {
        Transform Transform { get; }
        bool IsPrepared { get; }
        IReadOnlyList<ActorCommandInputBinding> Bindings { get; }
        bool HasBinding(
            ActorCommandId commandId,
            ActorCommandTriggerKind triggerKind);
        void PrepareInputBindings(PlayerInput playerInput, string context);
        void BindCommandSink(ActorCommandId commandId, IActorCommandSink sink);
        void UnbindCommandSink(ActorCommandId commandId);
        void UnbindCommandSink(IActorCommandSink sink);
        void Clear();
    }
}
