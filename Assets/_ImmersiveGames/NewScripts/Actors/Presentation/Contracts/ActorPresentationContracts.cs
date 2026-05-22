using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Contracts
{
    public enum ActorPresentationRequiredness
    {
        Unknown = 0,
        Optional = 1,
        Required = 2
    }

    public enum ActorPresentationSlotKind
    {
        Unknown = 0,
        VisualRoot = 1,
        SkinRoot = 2,
        FxRoot = 3,
        UiAnchor = 4
    }

    public enum ActorPresentationReleasePolicy
    {
        Unknown = 0,
        KeepBound = 1,
        ReleaseOnActivityExit = 2,
        ReleaseOnRouteExit = 3
    }

    public enum ActorPresentationResetPolicy
    {
        Unknown = 0,
        None = 1,
        ReapplyResolvedPlan = 2
    }

    public enum ActorPresentationVariationPolicy
    {
        Unknown = 0,
        None = 1,
        DeterministicBySeed = 2
    }

    public enum ActorPresentationResultKind
    {
        Unknown = 0,
        Materialized = 1,
        Released = 2,
        SkippedOptional = 3,
        Failed = 4
    }

    [Serializable]
    public struct ActorPresentationSlotRequirement
    {
        [SerializeField] private ActorPresentationSlotKind slotKind;
        [SerializeField] private string slotId;
        [SerializeField] private ActorPresentationRequiredness requiredness;

        public ActorPresentationSlotRequirement(
            ActorPresentationSlotKind slotKind,
            string slotId,
            ActorPresentationRequiredness requiredness)
        {
            this.slotKind = slotKind;
            this.slotId = Normalize(slotId);
            this.requiredness = requiredness;
        }

        public ActorPresentationSlotKind SlotKind => slotKind;
        public string SlotId => Normalize(slotId);
        public ActorPresentationRequiredness Requiredness => requiredness;

        public bool IsRequired => requiredness == ActorPresentationRequiredness.Required;
        public bool IsOptional => requiredness == ActorPresentationRequiredness.Optional;

        public bool IsValid =>
            slotKind != ActorPresentationSlotKind.Unknown &&
            !string.IsNullOrWhiteSpace(SlotId) &&
            requiredness != ActorPresentationRequiredness.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    [Serializable]
    public struct ActorPresentationSlotBinding
    {
        [SerializeField] private ActorPresentationSlotKind slotKind;
        [SerializeField] private string slotId;
        [SerializeField] private Transform container;

        public ActorPresentationSlotBinding(
            ActorPresentationSlotKind slotKind,
            string slotId,
            Transform container)
        {
            this.slotKind = slotKind;
            this.slotId = Normalize(slotId);
            this.container = container;
        }

        public ActorPresentationSlotKind SlotKind => slotKind;
        public string SlotId => Normalize(slotId);
        public Transform Container => container;
        public bool HasContainer => container != null;

        public bool IsValid =>
            slotKind != ActorPresentationSlotKind.Unknown &&
            !string.IsNullOrWhiteSpace(SlotId) &&
            container != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationResolvedPlan
    {
        public ActorPresentationResolvedPlan(
            string activityIdentity,
            string actorId,
            string actorKind,
            string profileId,
            ActorPresentationRequiredness requiredness,
            ActorPresentationReleasePolicy releasePolicy,
            ActorPresentationResetPolicy resetPolicy,
            ActorPresentationVariationPolicy variationPolicy,
            int variationSeed,
            GameObject visualPrefab,
            ActorPresentationSlotKind primarySlotKind,
            string primarySlotId,
            IReadOnlyList<ActorPresentationSlotBinding> slots,
            string source,
            string reason)
        {
            ActivityIdentity = Normalize(activityIdentity);
            ActorId = Normalize(actorId);
            ActorKind = Normalize(actorKind);
            ProfileId = Normalize(profileId);
            Requiredness = requiredness;
            ReleasePolicy = releasePolicy;
            ResetPolicy = resetPolicy;
            VariationPolicy = variationPolicy;
            VariationSeed = variationSeed;
            VisualPrefab = visualPrefab;
            PrimarySlotKind = primarySlotKind;
            PrimarySlotId = Normalize(primarySlotId);
            Slots = slots ?? Array.Empty<ActorPresentationSlotBinding>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ActivityIdentity { get; }
        public string ActorId { get; }
        public string ActorKind { get; }
        public string ProfileId { get; }
        public ActorPresentationRequiredness Requiredness { get; }
        public ActorPresentationReleasePolicy ReleasePolicy { get; }
        public ActorPresentationResetPolicy ResetPolicy { get; }
        public ActorPresentationVariationPolicy VariationPolicy { get; }
        public int VariationSeed { get; }
        public GameObject VisualPrefab { get; }
        public ActorPresentationSlotKind PrimarySlotKind { get; }
        public string PrimarySlotId { get; }
        public IReadOnlyList<ActorPresentationSlotBinding> Slots { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsRequired => Requiredness == ActorPresentationRequiredness.Required;
        public bool IsOptional => Requiredness == ActorPresentationRequiredness.Optional;
        public bool HasVisualPrefab => VisualPrefab != null;
        public bool HasPrimarySlot =>
            PrimarySlotKind != ActorPresentationSlotKind.Unknown &&
            !string.IsNullOrWhiteSpace(PrimarySlotId);

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActivityIdentity) &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind) &&
            !string.IsNullOrWhiteSpace(ProfileId) &&
            Requiredness != ActorPresentationRequiredness.Unknown &&
            ReleasePolicy != ActorPresentationReleasePolicy.Unknown &&
            ResetPolicy != ActorPresentationResetPolicy.Unknown &&
            VariationPolicy != ActorPresentationVariationPolicy.Unknown &&
            Slots != null &&
            !string.IsNullOrWhiteSpace(Source);

        public bool TryGetPrimarySlot(out ActorPresentationSlotBinding binding)
        {
            binding = default;

            if (!HasPrimarySlot || Slots == null)
            {
                return false;
            }

            for (int index = 0; index < Slots.Count; index++)
            {
                ActorPresentationSlotBinding current = Slots[index];
                if (current.SlotKind == PrimarySlotKind &&
                    string.Equals(current.SlotId, PrimarySlotId, StringComparison.Ordinal))
                {
                    binding = current;
                    return current.IsValid;
                }
            }

            return false;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationRuntimeHandle
    {
        public ActorPresentationRuntimeHandle(
            ActorPresentationResolvedPlan resolvedPlan,
            GameObject presentationInstance,
            string source,
            string reason)
        {
            ResolvedPlan = resolvedPlan;
            PresentationInstance = presentationInstance;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPresentationResolvedPlan ResolvedPlan { get; }
        public GameObject PresentationInstance { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ResolvedPlan.IsValid &&
            PresentationInstance != null &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationMaterializationCommand
    {
        public ActorPresentationMaterializationCommand(
            ActorPresentationResolvedPlan resolvedPlan,
            string source,
            string reason)
        {
            ResolvedPlan = resolvedPlan;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPresentationResolvedPlan ResolvedPlan { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => ResolvedPlan.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationReadyFact
    {
        public ActorPresentationReadyFact(
            ActorPresentationRuntimeHandle runtimeHandle,
            string source,
            string reason)
        {
            RuntimeHandle = runtimeHandle;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPresentationRuntimeHandle RuntimeHandle { get; }
        public ActorPresentationResolvedPlan ResolvedPlan => RuntimeHandle.ResolvedPlan;
        public GameObject PresentationInstance => RuntimeHandle.PresentationInstance;
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RuntimeHandle.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationReleaseCommand
    {
        public ActorPresentationReleaseCommand(
            ActorPresentationRuntimeHandle runtimeHandle,
            string source,
            string reason)
        {
            RuntimeHandle = runtimeHandle;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPresentationRuntimeHandle RuntimeHandle { get; }
        public ActorPresentationResolvedPlan ResolvedPlan => RuntimeHandle.ResolvedPlan;
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RuntimeHandle.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationReleasedFact
    {
        public ActorPresentationReleasedFact(
            ActorPresentationRuntimeHandle runtimeHandle,
            string source,
            string reason)
        {
            RuntimeHandle = runtimeHandle;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPresentationRuntimeHandle RuntimeHandle { get; }
        public ActorPresentationResolvedPlan ResolvedPlan => RuntimeHandle.ResolvedPlan;
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RuntimeHandle.ResolvedPlan.IsValid && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorPresentationResult
    {
        private ActorPresentationResult(
            ActorPresentationResultKind kind,
            ActorPresentationReadyFact readyFact,
            ActorPresentationReleasedFact releasedFact,
            ActorPresentationResolvedPlan skippedPlan,
            string reasonCode,
            string message)
        {
            Kind = kind;
            ReadyFact = readyFact;
            ReleasedFact = releasedFact;
            SkippedPlan = skippedPlan;
            ReasonCode = Normalize(reasonCode);
            Message = Normalize(message);
        }

        public ActorPresentationResultKind Kind { get; }
        public ActorPresentationReadyFact ReadyFact { get; }
        public ActorPresentationReleasedFact ReleasedFact { get; }
        public ActorPresentationResolvedPlan SkippedPlan { get; }
        public string ReasonCode { get; }
        public string Message { get; }

        public bool IsSuccess => Kind == ActorPresentationResultKind.Materialized || Kind == ActorPresentationResultKind.Released;
        public bool IsSkippedOptional => Kind == ActorPresentationResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActorPresentationResultKind.Failed;

        public bool IsValid =>
            Kind != ActorPresentationResultKind.Unknown &&
            (Kind == ActorPresentationResultKind.Materialized ? ReadyFact.IsValid : true) &&
            (Kind == ActorPresentationResultKind.Released ? ReleasedFact.IsValid : true) &&
            (Kind == ActorPresentationResultKind.SkippedOptional ? SkippedPlan.IsValid : true) &&
            (Kind != ActorPresentationResultKind.Failed || !string.IsNullOrWhiteSpace(ReasonCode));

        public static ActorPresentationResult Materialized(ActorPresentationReadyFact readyFact, string message)
        {
            return new ActorPresentationResult(
                ActorPresentationResultKind.Materialized,
                readyFact,
                default,
                default,
                reasonCode: string.Empty,
                message: message);
        }

        public static ActorPresentationResult Released(ActorPresentationReleasedFact releasedFact, string message)
        {
            return new ActorPresentationResult(
                ActorPresentationResultKind.Released,
                default,
                releasedFact,
                default,
                reasonCode: string.Empty,
                message: message);
        }

        public static ActorPresentationResult SkippedOptional(ActorPresentationResolvedPlan plan, string reasonCode, string message)
        {
            return new ActorPresentationResult(
                ActorPresentationResultKind.SkippedOptional,
                default,
                default,
                plan,
                reasonCode,
                message);
        }

        public static ActorPresentationResult Failed(string reasonCode, string message)
        {
            return new ActorPresentationResult(
                ActorPresentationResultKind.Failed,
                default,
                default,
                default,
                reasonCode,
                message);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
