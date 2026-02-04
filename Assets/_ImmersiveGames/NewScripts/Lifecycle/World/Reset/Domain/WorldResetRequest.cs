using System;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain
{
    /// <summary>
    /// Request imutável para reset do WorldLifecycle.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            string contextSignature,
            string reason,
            string profileName,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null,
            bool isGameplayProfile = true)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            ProfileName = profileName ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            SourceSignature = sourceSignature ?? string.Empty;
            IsGameplayProfile = isGameplayProfile;
            CreatedUtc = DateTime.UtcNow;
        }

        public string ContextSignature { get; }

        public string SourceSignature { get; }

        public string Reason { get; }

        public string ProfileName { get; }

        public string TargetScene { get; }

        public WorldResetOrigin Origin { get; }

        public bool IsGameplayProfile { get; }

        public DateTime CreatedUtc { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(ContextSignature);

        public override string ToString()
        {
            return $"WorldResetRequest(Signature='{ContextSignature}', Reason='{Reason}', Profile='{ProfileName}', Target='{TargetScene}', Origin={Origin})";
        }
    }
}
