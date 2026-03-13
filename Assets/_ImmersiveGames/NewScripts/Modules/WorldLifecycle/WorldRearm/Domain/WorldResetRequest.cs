using System;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain
{
    /// <summary>
    /// Request imutavel para reset do WorldLifecycle.
    /// Carrega apenas dados necessarios para correlacao e observabilidade de alto valor.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            SourceSignature = sourceSignature ?? string.Empty;
            CreatedUtc = DateTime.UtcNow;
        }

        public string ContextSignature { get; }

        public string SourceSignature { get; }

        public string Reason { get; }

        public string TargetScene { get; }

        public WorldResetOrigin Origin { get; }

        public DateTime CreatedUtc { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(ContextSignature);

        public override string ToString()
        {
            return $"WorldResetRequest(Signature='{ContextSignature}', Reason='{Reason}', Target='{TargetScene}', Origin={Origin})";
        }
    }
}
