using System;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Domain
{
    /// <summary>
    /// Request imutável para reset do WorldReset.
    /// Carrega apenas dados necessários para correlação e observabilidade de alto valor.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null)
            : this(
                contextSignature,
                reason,
                targetScene,
                origin,
                SceneRouteId.None,
                LevelContextSignature.Empty,
                sourceSignature)
        {
        }

        public WorldResetRequest(
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            SceneRouteId macroRouteId,
            LevelContextSignature levelSignature,
            string sourceSignature = null)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            MacroRouteId = macroRouteId;
            LevelSignature = levelSignature;
            SourceSignature = sourceSignature ?? string.Empty;
            CreatedUtc = DateTime.UtcNow;
        }

        public string ContextSignature { get; }
        public string SourceSignature { get; }
        public string Reason { get; }
        public string TargetScene { get; }
        public WorldResetOrigin Origin { get; }
        public SceneRouteId MacroRouteId { get; }
        public LevelContextSignature LevelSignature { get; }
        public DateTime CreatedUtc { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(ContextSignature);

        public override string ToString()
        {
            return $"WorldResetRequest(Signature='{ContextSignature}', Reason='{Reason}', Target='{TargetScene}', Origin={Origin}, Route='{MacroRouteId}', LevelSignature='{LevelSignature}')";
        }
    }
}
