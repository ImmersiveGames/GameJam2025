using System;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain
{
    /// <summary>
    /// Request imutável para reset do WorldReset.
    /// Carrega apenas dados necessários para correlação e observabilidade de alto valor.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            ResetKind kind,
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null)
            : this(
                kind,
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
            ResetKind kind,
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            SceneRouteId macroRouteId,
            LevelContextSignature levelSignature,
            string sourceSignature = null)
        {
            Kind = kind;
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            MacroRouteId = macroRouteId;
            LevelSignature = levelSignature;
            SourceSignature = sourceSignature ?? string.Empty;
            CreatedUtc = DateTime.UtcNow;
        }

        public ResetKind Kind { get; }
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
            return $"WorldResetRequest(Kind='{Kind}', Signature='{ContextSignature}', Reason='{Reason}', Target='{TargetScene}', Origin={Origin}, Route='{MacroRouteId}', LevelSignature='{LevelSignature}')";
        }
    }
}
