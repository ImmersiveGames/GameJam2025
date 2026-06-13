using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeResetResult
    {
        public ActorAttributeResetOutcome Outcome { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActivityResetIntent ResetIntent { get; }
        public ActivityResetStateProfileKind StateProfileKind { get; }
        public int ResetAttributeCount { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorAttributeResetOutcome.Applied;
        public bool SkippedNoContent => Outcome == ActorAttributeResetOutcome.SkippedNoContent;
        public bool Rejected => Outcome == ActorAttributeResetOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeResetOutcome.Failed;

        private ActorAttributeResetResult(
            ActorAttributeResetOutcome outcome,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            int resetAttributeCount,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ResetIntent = resetIntent;
            StateProfileKind = stateProfileKind;
            ResetAttributeCount = resetAttributeCount < 0 ? 0 : resetAttributeCount;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeResetResult AppliedResult(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            int resetAttributeCount)
        {
            return new ActorAttributeResetResult(
                ActorAttributeResetOutcome.Applied,
                actorInstanceRuntimeId,
                resetIntent,
                stateProfileKind,
                resetAttributeCount,
                string.Empty);
        }

        public static ActorAttributeResetResult Skipped(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            string reason)
        {
            return new ActorAttributeResetResult(
                ActorAttributeResetOutcome.SkippedNoContent,
                actorInstanceRuntimeId,
                resetIntent,
                stateProfileKind,
                0,
                reason);
        }

        public static ActorAttributeResetResult Reject(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            string reason)
        {
            return new ActorAttributeResetResult(
                ActorAttributeResetOutcome.Rejected,
                actorInstanceRuntimeId,
                resetIntent,
                stateProfileKind,
                0,
                reason);
        }

        public static ActorAttributeResetResult Fail(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind,
            string reason)
        {
            return new ActorAttributeResetResult(
                ActorAttributeResetOutcome.Failed,
                actorInstanceRuntimeId,
                resetIntent,
                stateProfileKind,
                0,
                reason);
        }
    }
}
