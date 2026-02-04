#nullable enable
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.ContentSwap
{
    /// <summary>
    /// Implementação simples e segura para armazenar o ContentSwap atual e pending.
    /// Publica eventos para auditoria e permite logs padronizados.
    /// </summary>
    public sealed class ContentSwapContextService : IContentSwapContextService
    {
        private readonly object _lock = new();

        private ContentSwapPlan _current = ContentSwapPlan.None;
        private ContentSwapPlan _pending = ContentSwapPlan.None;

        /// <summary>
        /// Preferred constructor. Uses <see cref="DebugUtility"/> for standardized logging.
        /// </summary>
        public ContentSwapContextService() { }

        public ContentSwapPlan Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public ContentSwapPlan Pending
        {
            get
            {
                lock (_lock)
                {
                    return _pending;
                }
            }
        }

        public bool HasPending
        {
            get
            {
                lock (_lock)
                {
                    return _pending.IsValid;
                }
            }
        }

        public void SetPending(ContentSwapPlan plan, string reason)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<ContentSwapContextService>("[ContentSwapContext] Ignorando SetPending com ContentSwapPlan inválido.");
                return;
            }

            lock (_lock)
            {
                _pending = plan;
            }

            // Assinatura: ContentSwapPendingSet
            DebugUtility.Log<ContentSwapContextService>($"[ContentSwapContext] ContentSwapPendingSet plan='{plan}' reason='{Sanitize(reason)}'");
            EventBus<ContentSwapPendingSetEvent>.Raise(new ContentSwapPendingSetEvent(plan, reason));
        }

        public bool TryCommitPending(string reason, out ContentSwapPlan committed)
        {
            if (!TryTakePending(out ContentSwapPlan previous, out ContentSwapPlan next))
            {
                committed = ContentSwapPlan.None;
                return false;
            }

            // Assinatura: ContentSwapCommitted
            DebugUtility.Log<ContentSwapContextService>($"[ContentSwapContext] ContentSwapCommitted prev='{previous}' current='{next}' reason='{Sanitize(reason)}'");
            EventBus<ContentSwapCommittedEvent>.Raise(new ContentSwapCommittedEvent(previous, next, reason));
            committed = next;
            return true;
        }

        public void ClearPending(string reason)
        {
            if (!TryClearPendingInternal())
            {
                return;
            }

            DebugUtility.Log<ContentSwapContextService>($"[ContentSwapContext] ContentSwapPendingCleared reason='{Sanitize(reason)}'");
            EventBus<ContentSwapPendingClearedEvent>.Raise(new ContentSwapPendingClearedEvent(reason));
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();

        private bool TryTakePending(out ContentSwapPlan previous, out ContentSwapPlan next)
        {
            lock (_lock)
            {
                if (!_pending.IsValid)
                {
                    previous = ContentSwapPlan.None;
                    next = ContentSwapPlan.None;
                    return false;
                }

                previous = _current;
                next = _pending;

                _current = next;
                _pending = ContentSwapPlan.None;
            }

            return true;
        }

        private bool TryClearPendingInternal()
        {
            lock (_lock)
            {
                if (!_pending.IsValid)
                {
                    return false;
                }

                _pending = ContentSwapPlan.None;
                return true;
            }
        }
    }
}
