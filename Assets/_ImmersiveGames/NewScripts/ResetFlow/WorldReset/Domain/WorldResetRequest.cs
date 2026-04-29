using System;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain
{
    /// <summary>
    /// Request imutável para reset do WorldReset.
    /// Carrega apenas dados necessários para correlação e observabilidade de alto valor.
    /// </summary>
    public readonly struct WorldResetRequest
    {
        public WorldResetRequest(
            ResetKind kind,
            WorldResetCorrelationKey correlationKey,
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            string sourceSignature = null,
            bool shouldExecute = true)
            : this(
                kind,
                correlationKey,
                contextSignature,
                reason,
                targetScene,
                origin,
                SceneRouteId.None,
                PhaseContextSignature.Empty,
                sourceSignature,
                shouldExecute)
        {
        }

        public WorldResetRequest(
            ResetKind kind,
            WorldResetCorrelationKey correlationKey,
            string contextSignature,
            string reason,
            string targetScene,
            WorldResetOrigin origin,
            SceneRouteId macroRouteId,
            PhaseContextSignature phaseSignature,
            string sourceSignature = null,
            bool shouldExecute = true)
        {
            Kind = kind;
            CorrelationKey = correlationKey;
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Origin = origin;
            MacroRouteId = macroRouteId;
            PhaseSignature = phaseSignature;
            SourceSignature = sourceSignature ?? string.Empty;
            ShouldExecute = shouldExecute;
            CreatedUtc = DateTime.UtcNow;
        }

        public ResetKind Kind { get; }
        public WorldResetCorrelationKey CorrelationKey { get; }
        public string ContextSignature { get; }
        public string SourceSignature { get; }
        public string Reason { get; }
        public string TargetScene { get; }
        public WorldResetOrigin Origin { get; }
        public SceneRouteId MacroRouteId { get; }
        public PhaseContextSignature PhaseSignature { get; }
        public bool ShouldExecute { get; }
        public DateTime CreatedUtc { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(ContextSignature);
        public bool HasCorrelationKey => CorrelationKey.IsValid;

        public override string ToString()
        {
            return $"WorldResetRequest(Kind='{Kind}', CorrelationKey='{CorrelationKey}', Signature='{ContextSignature}', Reason='{Reason}', Target='{TargetScene}', Origin={Origin}, Route='{MacroRouteId}', PhaseSignature='{PhaseSignature}', ShouldExecute='{ShouldExecute}')";
        }
    }
}

