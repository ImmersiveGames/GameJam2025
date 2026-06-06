using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public enum ActorCommandSourceKind
    {
        Unknown = 0,
        PlayerInput = 1
    }

    public enum ActorCommandValueKind
    {
        Unknown = 0,
        Move = 1,
        FirePrimary = 2
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
    public sealed class ActorCommandInputBinding
    {
        public string BindingId = string.Empty;
        public bool Enabled = true;
        public bool Required = false;
        public ActorCommandSourceKind SourceKind = ActorCommandSourceKind.PlayerInput;
        public ActorCommandValueKind ValueKind = ActorCommandValueKind.Move;
        public ActorCommandTriggerKind TriggerKind = ActorCommandTriggerKind.Continuous;
        [HideInInspector]
        public InputActionReference ActionReference;
        public string ActionMapName = string.Empty;
        public string ActionName = string.Empty;

        public bool IsConfigured =>
            SourceKind != ActorCommandSourceKind.Unknown &&
            ValueKind != ActorCommandValueKind.Unknown &&
            TriggerKind != ActorCommandTriggerKind.Unknown &&
            (ActionReference != null || (!string.IsNullOrWhiteSpace(ActionMapName) && !string.IsNullOrWhiteSpace(ActionName)));

        public bool IsActive => Enabled && IsConfigured;

        public bool Matches(
            ActorCommandSourceKind sourceKind,
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind)
        {
            return IsActive &&
                SourceKind == sourceKind &&
                ValueKind == valueKind &&
                TriggerKind == triggerKind;
        }

        public string ResolveBindingId()
        {
            if (!string.IsNullOrWhiteSpace(BindingId))
            {
                return BindingId.Trim();
            }

            string map = string.IsNullOrWhiteSpace(ActionMapName) ? "unmapped" : ActionMapName.Trim();
            string action = string.IsNullOrWhiteSpace(ActionName) ? "unbound" : ActionName.Trim();
            return $"{SourceKind}|{ValueKind}|{TriggerKind}|{map}.{action}";
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
            return new ActorCommandValue(ActorCommandValueKind.Move, triggerKind, value, boolValue: false, floatValue: 0f);
        }

        public static ActorCommandValue CreateFirePrimary(bool pressed, ActorCommandTriggerKind triggerKind = ActorCommandTriggerKind.Pressed)
        {
            return new ActorCommandValue(ActorCommandValueKind.FirePrimary, triggerKind, Vector2.zero, pressed, floatValue: 0f);
        }

        public static ActorCommandValue CreateButton(
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind,
            bool pressed)
        {
            return new ActorCommandValue(valueKind, triggerKind, Vector2.zero, pressed, floatValue: 0f);
        }
    }

    public readonly struct ActorCommandId
    {
        public ActorCommandId(
            ActorCommandSourceKind sourceKind,
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind,
            int sequence)
        {
            SourceKind = sourceKind;
            ValueKind = valueKind;
            TriggerKind = triggerKind;
            Sequence = sequence < 0 ? 0 : sequence;
        }

        public ActorCommandSourceKind SourceKind { get; }
        public ActorCommandValueKind ValueKind { get; }
        public ActorCommandTriggerKind TriggerKind { get; }
        public int Sequence { get; }

        public bool IsValid =>
            SourceKind != ActorCommandSourceKind.Unknown &&
            ValueKind != ActorCommandValueKind.Unknown &&
            TriggerKind != ActorCommandTriggerKind.Unknown &&
            Sequence >= 0;
    }

    public readonly struct ActorCommandEnvelope
    {
        public ActorCommandEnvelope(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorCommandId commandId,
            string sourceId,
            ActorCommandValue value,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            CommandId = commandId;
            SourceId = Normalize(sourceId);
            Value = value;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCommandId CommandId { get; }
        public string SourceId { get; }
        public ActorCommandValue Value { get; }
        public string Source { get; }
        public string Reason { get; }
        public ActorCommandSourceKind SourceKind => CommandId.SourceKind;

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            CommandId.IsValid &&
            Value.IsValid &&
            CommandId.SourceKind == SourceKind &&
            CommandId.ValueKind == Value.ValueKind &&
            CommandId.TriggerKind == Value.TriggerKind &&
            !string.IsNullOrWhiteSpace(SourceId);

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
            ActorCommandSourceKind sourceKind,
            ActorCommandValueKind valueKind,
            ActorCommandTriggerKind triggerKind);
        void PrepareInputBindings(PlayerInput playerInput, string context);
        void BindCommandSink(ActorCommandValueKind commandId, IActorCommandSink sink);
        void UnbindCommandSink(ActorCommandValueKind commandId);
        void UnbindCommandSink(IActorCommandSink sink);
        void Clear();
    }
}
