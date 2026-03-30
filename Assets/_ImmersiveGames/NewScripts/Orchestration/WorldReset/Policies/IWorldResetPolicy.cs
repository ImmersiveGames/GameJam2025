namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies
{
    /// <summary>
    /// Policy central para reset (Strict/Release + fallback/degraded).
    /// </summary>
    public interface IWorldResetPolicy
    {
        string Name { get; }

        bool IsStrict { get; }

        bool AllowSceneScan { get; }

        void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null);
    }
}
