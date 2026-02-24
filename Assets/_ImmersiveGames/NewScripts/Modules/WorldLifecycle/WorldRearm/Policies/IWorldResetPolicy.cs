namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies
{
    /// <summary>
    /// Policy central para reset (Strict/Release + fallback/degraded).
    /// </summary>
    public interface IWorldResetPolicy
    {
        string Name { get; }

        bool IsStrict { get; }

        bool AllowSceneScan { get; }

        bool AllowLegacyActorKindFallback { get; }

        void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null);
    }
}
