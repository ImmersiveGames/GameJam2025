#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Implementação simples e segura para armazenar o ContentSwap atual e pending.
    /// Publica eventos para auditoria e permite logs padronizados.
    /// </summary>
    public sealed class PhaseContextService : IPhaseContextService
    {
        private readonly object _lock = new();

        private PhasePlan _current = PhasePlan.None;
        private PhasePlan _pending = PhasePlan.None;

        /// <summary>
        /// Preferred constructor. Uses <see cref="DebugUtility"/> for standardized logging.
        /// </summary>
        public PhaseContextService() { }

        public PhasePlan Current
        {
            get { lock (_lock) return _current; }
        }

        public PhasePlan Pending
        {
            get { lock (_lock) return _pending; }
        }

        public bool HasPending
        {
            get { lock (_lock) return _pending.IsValid; }
        }

        public void SetPending(PhasePlan plan, string reason)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<PhaseContextService>("[PhaseContext] Ignorando SetPending com PhasePlan inválido.");
                return;
            }

            lock (_lock)
            {
                _pending = plan;
            }

            // Assinatura: PhasePendingSet
            DebugUtility.Log<PhaseContextService>($"[PhaseContext] PhasePendingSet plan='{plan}' reason='{Sanitize(reason)}'");
            EventBus<PhasePendingSetEvent>.Raise(new PhasePendingSetEvent(plan, reason));
        }

        public bool TryCommitPending(string reason, out PhasePlan committed)
        {
            PhasePlan previous;
            PhasePlan next;

            lock (_lock)
            {
                if (!_pending.IsValid)
                {
                    committed = PhasePlan.None;
                    return false;
                }

                previous = _current;
                next = _pending;

                _current = next;
                _pending = PhasePlan.None;
            }

            committed = next;

            // Assinatura: PhaseCommitted
            DebugUtility.Log<PhaseContextService>($"[PhaseContext] PhaseCommitted prev='{previous}' current='{next}' reason='{Sanitize(reason)}'");
            EventBus<PhaseCommittedEvent>.Raise(new PhaseCommittedEvent(previous, next, reason));
            return true;
        }

        public void ClearPending(string reason)
        {
            bool hadPending;
            lock (_lock)
            {
                hadPending = _pending.IsValid;
                _pending = PhasePlan.None;
            }

            if (!hadPending) return;

            DebugUtility.Log<PhaseContextService>($"[PhaseContext] PhasePendingCleared reason='{Sanitize(reason)}'");
            EventBus<PhasePendingClearedEvent>.Raise(new PhasePendingClearedEvent(reason));
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
