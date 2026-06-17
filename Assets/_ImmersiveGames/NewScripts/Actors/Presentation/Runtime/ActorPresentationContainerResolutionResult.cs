using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    public enum ActorPresentationContainerResolutionResultKind
    {
        Unknown = 0,
        Success = 1,
        SkippedOptional = 2,
        Failed = 3
    }

    public readonly struct ActorPresentationSkippedSlot
    {
        public ActorPresentationSkippedSlot(
            ActorPresentationSlotRequirement requirement,
            string reasonCode,
            string message)
        {
            Requirement = requirement;
            ReasonCode = Normalize(reasonCode);
            Message = Normalize(message);
        }

        public ActorPresentationSlotRequirement Requirement { get; }
        public string ReasonCode { get; }
        public string Message { get; }

        public bool IsValid =>
            Requirement.IsValid &&
            !string.IsNullOrWhiteSpace(ReasonCode);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPresentationContainerResolutionResult
    {
        private ActorPresentationContainerResolutionResult(
            ActorPresentationContainerResolutionResultKind kind,
            IReadOnlyList<ActorPresentationSlotBinding> bindings,
            IReadOnlyList<ActorPresentationSkippedSlot> skippedOptionalSlots,
            string reasonCode,
            string message)
        {
            Kind = kind;
            Bindings = bindings ?? Array.Empty<ActorPresentationSlotBinding>();
            SkippedOptionalSlots = skippedOptionalSlots ?? Array.Empty<ActorPresentationSkippedSlot>();
            ReasonCode = Normalize(reasonCode);
            Message = Normalize(message);
        }

        public ActorPresentationContainerResolutionResultKind Kind { get; }
        public IReadOnlyList<ActorPresentationSlotBinding> Bindings { get; }
        public IReadOnlyList<ActorPresentationSkippedSlot> SkippedOptionalSlots { get; }
        public string ReasonCode { get; }
        public string Message { get; }

        public bool IsSuccess => Kind == ActorPresentationContainerResolutionResultKind.Success;
        public bool IsSkippedOptional => Kind == ActorPresentationContainerResolutionResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActorPresentationContainerResolutionResultKind.Failed;
        public bool HasSkippedOptionalSlots => SkippedOptionalSlots.Count > 0;

        public bool IsValid =>
            Kind != ActorPresentationContainerResolutionResultKind.Unknown &&
            (Kind == ActorPresentationContainerResolutionResultKind.Failed
                ? !string.IsNullOrWhiteSpace(ReasonCode)
                : true);

        public static ActorPresentationContainerResolutionResult Success(
            IReadOnlyList<ActorPresentationSlotBinding> bindings,
            IReadOnlyList<ActorPresentationSkippedSlot> skippedOptionalSlots,
            string message)
        {
            return new ActorPresentationContainerResolutionResult(
                ActorPresentationContainerResolutionResultKind.Success,
                bindings,
                skippedOptionalSlots,
                reasonCode: string.Empty,
                message: message);
        }

        public static ActorPresentationContainerResolutionResult SkippedOptional(
            IReadOnlyList<ActorPresentationSkippedSlot> skippedOptionalSlots,
            string reasonCode,
            string message)
        {
            return new ActorPresentationContainerResolutionResult(
                ActorPresentationContainerResolutionResultKind.SkippedOptional,
                Array.Empty<ActorPresentationSlotBinding>(),
                skippedOptionalSlots,
                reasonCode,
                message);
        }

        public static ActorPresentationContainerResolutionResult Failed(
            string reasonCode,
            string message)
        {
            return new ActorPresentationContainerResolutionResult(
                ActorPresentationContainerResolutionResultKind.Failed,
                Array.Empty<ActorPresentationSlotBinding>(),
                Array.Empty<ActorPresentationSkippedSlot>(),
                reasonCode,
                message);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
