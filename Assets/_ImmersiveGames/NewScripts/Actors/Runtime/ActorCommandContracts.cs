using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public enum ActorCommandKind
    {
        Unknown = 0,
        Move = 1,
        FirePrimary = 2
    }

    public enum ActorCommandSourceKind
    {
        Unknown = 0,
        PlayerInput = 1
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
            Value = Normalize(value);
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public readonly struct ActorCommandSourceIdentity
    {
        public ActorCommandSourceIdentity(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class ActorCommandInputBinding
    {
        public string BindingId = string.Empty;
        public bool Enabled = true;
        public bool Required = false;
        public ActorCommandSourceKind SourceKind = ActorCommandSourceKind.PlayerInput;
        public ActorCommandKind CommandKind = ActorCommandKind.Move;
        public ActorCommandValueKind ValueKind = ActorCommandValueKind.Vector2;
        public ActorCommandTriggerKind TriggerKind = ActorCommandTriggerKind.Continuous;
        [HideInInspector]
        public InputActionReference ActionReference;
        public string ActionMapName = string.Empty;
        public string ActionName = string.Empty;

        public bool IsConfigured =>
            HasBindingId &&
            SourceKind != ActorCommandSourceKind.Unknown &&
            CommandKind != ActorCommandKind.Unknown &&
            ValueKind != ActorCommandValueKind.Unknown &&
            TriggerKind != ActorCommandTriggerKind.Unknown &&
            IsCommandValueShapeValid &&
            (ActionReference != null || (!string.IsNullOrWhiteSpace(ActionMapName) && !string.IsNullOrWhiteSpace(ActionName)));

        public bool IsActive => Enabled && IsConfigured;

        public bool HasBindingId => !string.IsNullOrWhiteSpace(BindingId);

        public bool IsCommandValueShapeValid => IsCommandValueCompatible(ResolveCommandId(), ValueKind, TriggerKind);

        public bool Matches(
            ActorCommandId commandId,
            ActorCommandSourceKind sourceKind,
            ActorCommandTriggerKind triggerKind)
        {
            return IsActive &&
                TryResolveCommandId(out ActorCommandId declaredCommandId) &&
                declaredCommandId == commandId &&
                SourceKind == sourceKind &&
                TriggerKind == triggerKind;
        }

        public bool TryResolveCommandId(out ActorCommandId commandId)
        {
            commandId = ResolveCommandId();
            return commandId.IsValid;
        }

        public ActorCommandId ResolveCommandIdOrFail()
        {
            if (!TryResolveCommandId(out ActorCommandId commandId))
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
            ActorCommandSourceKind sourceKind,
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
            SourceKind = sourceKind;
            Value = value;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCommandId CommandId { get; }
        public ActorCommandBindingId BindingId { get; }
        public ActorCommandSourceIdentity SourceIdentity { get; }
        public int Sequence { get; }
        public ActorCommandSourceKind SourceKind { get; }
        public ActorCommandValue Value { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            CommandId.IsValid &&
            BindingId.IsValid &&
            SourceIdentity.IsValid &&
            SourceKind != ActorCommandSourceKind.Unknown &&
            Value.IsValid &&
            ActorCommandInputBinding.IsCommandValueCompatible(CommandId, Value.ValueKind, Value.TriggerKind) &&
            Sequence >= 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
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
            Reason = Normalize(reason);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            ActorCommandSourceKind sourceKind,
            ActorCommandTriggerKind triggerKind);
        void PrepareInputBindings(PlayerInput playerInput, string context);
        void BindCommandSink(ActorCommandId commandId, IActorCommandSink sink);
        void UnbindCommandSink(ActorCommandId commandId);
        void UnbindCommandSink(IActorCommandSink sink);
        void Clear();
    }
}
