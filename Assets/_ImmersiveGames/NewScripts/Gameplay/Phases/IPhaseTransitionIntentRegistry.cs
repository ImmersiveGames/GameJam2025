#nullable enable
using System;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Registry global para intenção de troca de fase durante SceneFlow.
    /// </summary>
    public interface IPhaseTransitionIntentRegistry
    {
        bool TrySet(string contextSignature, PhasePlan plan, string reason, DateTime? timestampUtc = null);
        bool TryConsume(string contextSignature, out PhaseTransitionIntent intent);
        void Clear(string contextSignature);
    }

    public readonly struct PhaseTransitionIntent
    {
        public PhasePlan Plan { get; }
        public string Reason { get; }
        public DateTime? TimestampUtc { get; }

        public PhaseTransitionIntent(PhasePlan plan, string reason, DateTime? timestampUtc)
        {
            Plan = plan;
            Reason = reason ?? string.Empty;
            TimestampUtc = timestampUtc;
        }
    }
}
