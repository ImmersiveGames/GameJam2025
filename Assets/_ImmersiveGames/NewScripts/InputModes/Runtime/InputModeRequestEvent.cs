namespace ImmersiveGames.GameJam2025.Infrastructure.InputModes.Runtime
{
    public enum InputModeRequestKind
    {
        Unspecified = 0,
        FrontendMenu = 1,
        Gameplay = 2,
        PauseOverlay = 3
    }
    public readonly struct InputModeRequestEvent
    {
        public InputModeRequestKind Kind { get; }
        public string Reason { get; }
        public string Source { get; }
        public string ContextSignature { get; }
        public InputModeRequestEvent(
            InputModeRequestKind kind,
            string reason,
            string source,
            string contextSignature = "")
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
        }
    }
}

