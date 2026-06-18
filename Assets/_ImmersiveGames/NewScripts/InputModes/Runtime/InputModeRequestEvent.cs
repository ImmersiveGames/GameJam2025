using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    public enum InputModeRequestKind
    {
        Unspecified = 0,
        FrontendMenu = 1,
        Gameplay = 2,
        PauseOverlay = 3,
        InputLocked = 4,
    }
    public readonly struct InputModeRequestEvent
    {
        public InputModeRequestKind Kind { get; }
        public string Reason { get; }
        public string Source { get; }
        public string ContextSignature { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string InitialInputMode { get; }
        public InputModeRequestEvent(
            InputModeRequestKind kind,
            string reason,
            string source,
            string contextSignature,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string initialInputMode)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Source = source.TrimToEmpty();
            ContextSignature = contextSignature.TrimToEmpty();
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            InitialInputMode = initialInputMode.TrimToEmpty();
        }
    }
}

