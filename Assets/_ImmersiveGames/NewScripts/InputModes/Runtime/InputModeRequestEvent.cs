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
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RouteIdentity = string.IsNullOrWhiteSpace(routeIdentity) ? string.Empty : routeIdentity.Trim();
            RouteOperationId = string.IsNullOrWhiteSpace(routeOperationId) ? string.Empty : routeOperationId.Trim();
            TransitionId = string.IsNullOrWhiteSpace(transitionId) ? string.Empty : transitionId.Trim();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            InitialInputMode = string.IsNullOrWhiteSpace(initialInputMode) ? string.Empty : initialInputMode.Trim();
        }
    }
}

