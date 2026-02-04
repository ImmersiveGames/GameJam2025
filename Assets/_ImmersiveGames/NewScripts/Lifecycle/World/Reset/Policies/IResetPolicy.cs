namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies
{
    /// <summary>
    /// Policy central para reset (Strict/Release + fallback/degraded).
    /// </summary>
    public interface IResetPolicy
    {
        string Name { get; }

        bool IsStrict { get; }

        bool AllowSceneScan { get; }

        bool AllowLegacyActorKindFallback { get; }

        void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null);
    }
}
