#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseTransitionIntentRegistry : IPhaseTransitionIntentRegistry
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, PhaseTransitionIntent> _intents = new();

        public bool TrySet(string contextSignature, PhasePlan plan, string reason, DateTime? timestampUtc = null)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            if (!plan.IsValid)
            {
                return false;
            }

            var signature = contextSignature.Trim();
            var intent = new PhaseTransitionIntent(plan, reason ?? string.Empty, timestampUtc ?? DateTime.UtcNow);

            lock (_lock)
            {
                if (_intents.ContainsKey(signature))
                {
                    return false;
                }

                _intents[signature] = intent;
            }

            DebugUtility.Log<PhaseTransitionIntentRegistry>(
                $"[PhaseIntent] Set sig={signature} plan='{plan}' reason='{Sanitize(reason)}'");
            return true;
        }

        public bool TryConsume(string contextSignature, out PhaseTransitionIntent intent)
        {
            intent = default;

            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            var signature = contextSignature.Trim();

            lock (_lock)
            {
                if (!_intents.TryGetValue(signature, out intent))
                {
                    return false;
                }

                _intents.Remove(signature);
            }

            DebugUtility.Log<PhaseTransitionIntentRegistry>(
                $"[PhaseIntent] Consumed sig={signature} plan='{intent.Plan}'");
            return true;
        }

        public void Clear(string contextSignature)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return;
            }

            var signature = contextSignature.Trim();

            lock (_lock)
            {
                _intents.Remove(signature);
            }
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
