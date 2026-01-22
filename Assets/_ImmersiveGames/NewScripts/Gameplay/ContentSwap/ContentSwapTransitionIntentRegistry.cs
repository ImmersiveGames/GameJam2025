#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ContentSwapTransitionIntentRegistry : IContentSwapTransitionIntentRegistry
    {
        private readonly object _lock = new();
        private bool _hasIntent;
        private ContentSwapTransitionIntent _intent;

        public bool RegisterIntent(ContentSwapTransitionIntent intent)
        {
            if (!intent.Plan.IsValid)
            {
                return false;
            }

            if (intent.Mode != ContentSwapMode.SceneTransition)
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

            DebugUtility.Log<ContentSwapTransitionIntentRegistry>(
                $"[ContentSwapIntent] Registered sig='{intent.SourceSignature}' plan='{intent.Plan}' mode='{intent.Mode}' reason='{Sanitize(intent.Reason)}'");
            return true;
        }

        public bool TryConsumeIntent(out ContentSwapTransitionIntent intent)
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

            DebugUtility.Log<ContentSwapTransitionIntentRegistry>(
                $"[ContentSwapIntent] Consumed sig='{intent.SourceSignature}' plan='{intent.Plan}' mode='{intent.Mode}'");
            return true;
        }

        public bool TryPeekIntent(out ContentSwapTransitionIntent intent)
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
            ContentSwapTransitionIntent clearedIntent;

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

            DebugUtility.LogWarning<ContentSwapTransitionIntentRegistry>(
                $"[ContentSwapIntent] Cleared sig='{clearedIntent.SourceSignature}' reason='{Sanitize(reason)}'");
        }

        public bool TrySet(string contextSignature, ContentSwapPlan plan, string reason, DateTime? timestampUtc = null)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            var signature = contextSignature.Trim();

            return RegisterIntent(new ContentSwapTransitionIntent(
                plan,
                ContentSwapMode.SceneTransition,
                reason ?? string.Empty,
                signature,
                string.Empty,
                string.Empty,
                timestampUtc ?? DateTime.UtcNow));
        }

        public bool TryConsume(string contextSignature, out ContentSwapTransitionIntent intent)
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
