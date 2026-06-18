using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public readonly struct ActorAttributeUiBindingResult
    {
        public ActorAttributeUiBindingResult(
            ActorAttributeUiBindingResultKind kind,
            ActorAttributeUiBindingHandle handle,
            string failureReason)
        {
            Kind = kind;
            Handle = handle;
            FailureReason = failureReason.TrimToEmpty();
        }

        public ActorAttributeUiBindingResultKind Kind { get; }
        public ActorAttributeUiBindingHandle Handle { get; }
        public string FailureReason { get; }

        public bool IsBound => Kind == ActorAttributeUiBindingResultKind.Bound && Handle != null;
        public bool IsRejected => !IsBound;

        public static ActorAttributeUiBindingResult Bound(ActorAttributeUiBindingHandle handle)
        {
            return new ActorAttributeUiBindingResult(ActorAttributeUiBindingResultKind.Bound, handle, string.Empty);
        }

        public static ActorAttributeUiBindingResult Rejected(
            ActorAttributeUiBindingResultKind kind,
            string failureReason)
        {
            return new ActorAttributeUiBindingResult(kind, null, failureReason);
        }
    }
}
