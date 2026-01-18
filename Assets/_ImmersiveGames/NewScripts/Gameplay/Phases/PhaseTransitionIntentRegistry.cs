#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseTransitionIntentRegistry : IPhaseTransitionIntentRegistry
    {
        private readonly object _lock = new();
        private bool _hasIntent;
        private PhaseTransitionIntent _intent;

        public bool RegisterIntent(PhaseTransitionIntent intent)
        {
            if (!intent.Plan.IsValid)
            {
                return false;
            }

            if (intent.Mode != PhaseChangeMode.SceneTransition)
            {
                return false;
            }

            lock (_lock)
            {
                if (_hasIntent)
                {
                    return false;
                }

                _intent = intent;
                _hasIntent = true;
            }

            DebugUtility.Log<PhaseTransitionIntentRegistry>(
                $"[PhaseIntent] Registered sig='{intent.SourceSignature}' plan='{intent.Plan}' mode='{intent.Mode}' reason='{Sanitize(intent.Reason)}'");
            return true;
        }

        public bool TryConsumeIntent(out PhaseTransitionIntent intent)
        {
            intent = default;

            lock (_lock)
            {
                if (!_hasIntent)
                {
                    return false;
                }

                intent = _intent;
                _intent = default;
                _hasIntent = false;
            }

            DebugUtility.Log<PhaseTransitionIntentRegistry>(
                $"[PhaseIntent] Consumed sig='{intent.SourceSignature}' plan='{intent.Plan}' mode='{intent.Mode}'");
            return true;
        }

        public bool TryPeekIntent(out PhaseTransitionIntent intent)
        {
            intent = default;

            lock (_lock)
            {
                if (!_hasIntent)
                {
                    return false;
                }

                intent = _intent;
                return true;
            }
        }

        public void ClearIntent(string reason)
        {
            bool cleared;
            PhaseTransitionIntent clearedIntent;

            lock (_lock)
            {
                cleared = _hasIntent;
                clearedIntent = _intent;
                _intent = default;
                _hasIntent = false;
            }

            if (!cleared)
            {
                return;
            }

            DebugUtility.LogWarning<PhaseTransitionIntentRegistry>(
                $"[PhaseIntent] Cleared sig='{clearedIntent.SourceSignature}' reason='{Sanitize(reason)}'");
        }

        public bool TrySet(string contextSignature, PhasePlan plan, string reason, DateTime? timestampUtc = null)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            var signature = contextSignature.Trim();

            return RegisterIntent(new PhaseTransitionIntent(
                plan,
                PhaseChangeMode.SceneTransition,
                reason ?? string.Empty,
                signature,
                string.Empty,
                string.Empty,
                timestampUtc ?? DateTime.UtcNow));
        }

        public bool TryConsume(string contextSignature, out PhaseTransitionIntent intent)
        {
            intent = default;

            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            var signature = contextSignature.Trim();

            if (!TryPeekIntent(out var peek))
            {
                return false;
            }

            if (!string.Equals(peek.SourceSignature, signature, StringComparison.Ordinal))
            {
                return false;
            }

            return TryConsumeIntent(out intent);
        }

        public void Clear(string contextSignature)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return;
            }

            var signature = contextSignature.Trim();

            if (!TryPeekIntent(out var peek))
            {
                return;
            }

            if (!string.Equals(peek.SourceSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            ClearIntent($"SignatureClear sig='{signature}'");
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
