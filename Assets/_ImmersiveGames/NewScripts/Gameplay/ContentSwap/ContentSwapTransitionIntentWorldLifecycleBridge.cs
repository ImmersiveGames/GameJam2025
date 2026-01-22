#nullable enable
// Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapTransitionIntentWorldLifecycleBridge.cs
// Bridge: aplica/commita ContentSwapTransitionIntent no ponto seguro do WorldLifecycle (ResetCompleted).

using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ContentSwapTransitionIntentWorldLifecycleBridge : IDisposable
    {
        private readonly IContentSwapTransitionIntentRegistry _intentRegistry;
        private readonly IContentSwapContextService _contentSwapContext;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _resetCompletedBinding;
        private bool _registered;

        public ContentSwapTransitionIntentWorldLifecycleBridge(
            IContentSwapTransitionIntentRegistry intentRegistry,
            IContentSwapContextService contentSwapContext)
        {
            _intentRegistry = intentRegistry ?? throw new ArgumentNullException(nameof(intentRegistry));
            _contentSwapContext = contentSwapContext ?? throw new ArgumentNullException(nameof(contentSwapContext));

            _resetCompletedBinding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnResetCompleted);
            Register();
        }

        public void Dispose() => Unregister();

        private void Register()
        {
            if (_registered) return;

            EventBus<WorldLifecycleResetCompletedEvent>.Register(_resetCompletedBinding);
            _registered = true;

            DebugUtility.LogVerbose<ContentSwapTransitionIntentWorldLifecycleBridge>(
                "[ContentSwapIntentBridge] Binding registrado (WorldLifecycleResetCompletedEvent).",
                DebugUtility.Colors.Info);
        }

        private void Unregister()
        {
            if (!_registered) return;

            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_resetCompletedBinding);
            _registered = false;

            DebugUtility.LogVerbose<ContentSwapTransitionIntentWorldLifecycleBridge>(
                "[ContentSwapIntentBridge] Binding removido (WorldLifecycleResetCompletedEvent).",
                DebugUtility.Colors.Info);
        }

        private void OnResetCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            var signature = evt.ContextSignature ?? string.Empty;
            if (string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            if (!_intentRegistry.TryConsume(signature, out var intent))
            {
                return;
            }

            DebugUtility.Log<ContentSwapTransitionIntentWorldLifecycleBridge>(
                $"[ContentSwapIntentBridge] ResetCompleted -> consumindo intent e aplicando conteúdo. signature='{signature}', contentId='{intent.Plan.ContentId}', reason='{Sanitize(intent.Reason)}'.",
                DebugUtility.Colors.Info);

            _contentSwapContext.SetPending(intent.Plan, intent.Reason);

            if (!_contentSwapContext.TryCommitPending(intent.Reason, out var committed))
            {
                DebugUtility.LogWarning<ContentSwapTransitionIntentWorldLifecycleBridge>(
                    $"[ContentSwapIntentBridge] Intent consumido, mas commit falhou (sem pending?). signature='{signature}', plan='{intent.Plan}'.");
                return;
            }

            // ContentSwapCommittedEvent é publicado pelo ContentSwapContextService.
            DebugUtility.LogVerbose<ContentSwapTransitionIntentWorldLifecycleBridge>(
                $"[ContentSwapIntentBridge] Commit aplicado. signature='{signature}', committed='{committed}'.");
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
