#nullable enable
using System;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Registry global para intenção de troca de fase durante SceneFlow.
    /// </summary>
    public interface IPhaseTransitionIntentRegistry
    {
        bool RegisterIntent(PhaseTransitionIntent intent);
        bool TryConsumeIntent(out PhaseTransitionIntent intent);
        bool TryPeekIntent(out PhaseTransitionIntent intent);
        void ClearIntent(string reason);

        // Compatibilidade: assinatura explícita (legado).
        bool TrySet(string contextSignature, PhasePlan plan, string reason, DateTime? timestampUtc = null);
        bool TryConsume(string contextSignature, out PhaseTransitionIntent intent);
        void Clear(string contextSignature);
    }

    public readonly struct PhaseTransitionIntent
    {
        public PhasePlan Plan { get; }
        public PhaseChangeMode Mode { get; }
        public string Reason { get; }
        public string SourceSignature { get; }
        public string TransitionProfile { get; }
        public string TargetScene { get; }
        public DateTime? TimestampUtc { get; }

        public PhaseTransitionIntent(
            PhasePlan plan,
            PhaseChangeMode mode,
            string reason,
            string sourceSignature,
            string transitionProfile,
            string targetScene,
            DateTime? timestampUtc)
        {
            Plan = plan;
            Mode = mode;
            Reason = reason ?? string.Empty;
            SourceSignature = sourceSignature ?? string.Empty;
            TransitionProfile = transitionProfile ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            TimestampUtc = timestampUtc;
        }
    }
}
