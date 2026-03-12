using System;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain
{
    /// <summary>
    /// Request imutavel para reset do WorldLifecycle.
    /// Labels de profile, quando presentes, sao apenas observabilidade.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            string contextSignature,
            string reason,
            string profileName,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            ProfileName = profileName ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            SourceSignature = sourceSignature ?? string.Empty;
            CreatedUtc = DateTime.UtcNow;
        }

        public string ContextSignature { get; }

        public string SourceSignature { get; }

        public string Reason { get; }

        public string ProfileName { get; }

        public string TargetScene { get; }

        public WorldResetOrigin Origin { get; }

        public DateTime CreatedUtc { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(ContextSignature);

        public override string ToString()
        {
            return $"WorldResetRequest(Signature='{ContextSignature}', Reason='{Reason}', ProfileLabel='{ProfileName}', Target='{TargetScene}', Origin={Origin})";
        }
    }
}
