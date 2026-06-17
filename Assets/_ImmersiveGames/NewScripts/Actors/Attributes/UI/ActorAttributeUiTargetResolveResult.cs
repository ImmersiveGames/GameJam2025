using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public readonly struct ActorAttributeUiTargetResolveResult
    {
        public ActorAttributeUiTargetResolveResult(
            ActorAttributeUiTargetResolveResultKind kind,
            ActorAttributeUiTargetSelectorKind selectorKind,
            ActorAttributeUiBindingTarget target,
            string source,
            string reason,
            string failureReason)
        {
            Kind = kind;
            SelectorKind = selectorKind;
            Target = target;
            Source = Normalize(source);
            Reason = Normalize(reason);
            FailureReason = Normalize(failureReason);
        }

        public ActorAttributeUiTargetResolveResultKind Kind { get; }
        public ActorAttributeUiTargetSelectorKind SelectorKind { get; }
        public ActorAttributeUiBindingTarget Target { get; }
        public ActorId ActorId => Target.ActorId;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => Target.ActorInstanceRuntimeId;
        public ActorAttributeId AttributeId => Target.AttributeId;
        public string Source { get; }
        public string Reason { get; }
        public string FailureReason { get; }

        public bool IsResolved => Kind == ActorAttributeUiTargetResolveResultKind.Resolved && Target.IsValid;
        public bool IsRejected => !IsResolved;

        public static ActorAttributeUiTargetResolveResult Resolved(
            ActorAttributeUiTargetSelectorKind selectorKind,
            ActorAttributeUiBindingTarget target,
            string source,
            string reason)
        {
            return new ActorAttributeUiTargetResolveResult(
                ActorAttributeUiTargetResolveResultKind.Resolved,
                selectorKind,
                target,
                source,
                reason,
                string.Empty);
        }

        public static ActorAttributeUiTargetResolveResult Rejected(
            ActorAttributeUiTargetResolveResultKind kind,
            ActorAttributeUiTargetSelectorKind selectorKind,
            string source,
            string reason,
            string failureReason)
        {
            return new ActorAttributeUiTargetResolveResult(
                kind,
                selectorKind,
                default,
                source,
                reason,
                failureReason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
