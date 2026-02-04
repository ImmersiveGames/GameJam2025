namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain
{
    /// <summary>
    /// Resultado de guard/validação para o pipeline de reset.
    /// </summary>
    public readonly struct ResetDecision
    {
        private ResetDecision(bool shouldProceed, bool shouldPublishCompletion, bool isViolation, string reason, string detail)
        {
            ShouldProceed = shouldProceed;
            ShouldPublishCompletion = shouldPublishCompletion;
            IsViolation = isViolation;
            Reason = reason ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool ShouldProceed { get; }

        public bool ShouldPublishCompletion { get; }

        public bool IsViolation { get; }

        public string Reason { get; }

        public string Detail { get; }

        public static ResetDecision Proceed()
        {
            return new ResetDecision(true, shouldPublishCompletion: false, isViolation: false, reason: string.Empty, detail: string.Empty);
        }

        public static ResetDecision Skip(string reason, string detail = null, bool publishCompletion = true, bool isViolation = false)
        {
            return new ResetDecision(false, publishCompletion, isViolation, reason, detail);
        }
    }
}
