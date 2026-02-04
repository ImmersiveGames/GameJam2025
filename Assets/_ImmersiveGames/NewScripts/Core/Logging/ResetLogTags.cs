namespace _ImmersiveGames.NewScripts.Core.Logging
{
    /// <summary>
    /// Tags canônicas para logs de reset (World/Gameplay).
    /// Mantém âncoras consistentes e evita strings mágicas.
    /// </summary>
    public static class ResetLogTags
    {
        public const string Start = "ResetStart";
        public const string ValidationFailed = "Reset/ValidationFailed";
        public const string Guarded = "Reset/Guarded";
        public const string Skipped = "ResetSkip";
        public const string DegradedMode = "Reset/DegradedMode";
        public const string Recovered = "Reset/Recovered";
        public const string Failed = "ResetFailed";
        public const string Completed = "ResetCompleted";
    }
}
