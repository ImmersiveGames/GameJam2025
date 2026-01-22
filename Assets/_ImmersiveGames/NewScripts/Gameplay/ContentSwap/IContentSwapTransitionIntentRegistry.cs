#nullable enable
using System;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Registry global para intenção de troca de conteúdo durante SceneFlow.
    /// </summary>
    public interface IContentSwapTransitionIntentRegistry
    {
        bool RegisterIntent(ContentSwapTransitionIntent intent);
        bool TryConsumeIntent(out ContentSwapTransitionIntent intent);
        bool TryPeekIntent(out ContentSwapTransitionIntent intent);
        void ClearIntent(string reason);

        // Compatibilidade: assinatura explícita.
        bool TrySet(string contextSignature, ContentSwapPlan plan, string reason, DateTime? timestampUtc = null);
        bool TryConsume(string contextSignature, out ContentSwapTransitionIntent intent);
        void Clear(string contextSignature);
    }

    public readonly struct ContentSwapTransitionIntent
    {
        public ContentSwapPlan Plan { get; }
        public ContentSwapMode Mode { get; }
        public string Reason { get; }
        public string SourceSignature { get; }
        public string TransitionProfile { get; }
        public string TargetScene { get; }
        public DateTime? TimestampUtc { get; }

        public ContentSwapTransitionIntent(
            ContentSwapPlan plan,
            ContentSwapMode mode,
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
