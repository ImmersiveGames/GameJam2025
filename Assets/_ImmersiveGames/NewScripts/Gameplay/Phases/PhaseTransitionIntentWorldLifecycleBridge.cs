#nullable enable
// Assets/_ImmersiveGames/NewScripts/Gameplay/Phases/PhaseTransitionIntentWorldLifecycleBridge.cs
// Bridge: aplica/commita PhaseTransitionIntent no ponto seguro do WorldLifecycle (ResetCompleted).

using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseTransitionIntentWorldLifecycleBridge : IDisposable
    {
        private readonly IPhaseTransitionIntentRegistry _intentRegistry;
        private readonly IPhaseContextService _phaseContext;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _resetCompletedBinding;
        private bool _registered;

        public PhaseTransitionIntentWorldLifecycleBridge(
            IPhaseTransitionIntentRegistry intentRegistry,
            IPhaseContextService phaseContext)
        {
            _intentRegistry = intentRegistry ?? throw new ArgumentNullException(nameof(intentRegistry));
            _phaseContext = phaseContext ?? throw new ArgumentNullException(nameof(phaseContext));

            _resetCompletedBinding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnResetCompleted);
            Register();
        }

        public void Dispose() => Unregister();

        private void Register()
        {
            if (_registered) return;

            EventBus<WorldLifecycleResetCompletedEvent>.Register(_resetCompletedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PhaseTransitionIntentWorldLifecycleBridge>(
                "[PhaseIntentBridge] Binding registrado (WorldLifecycleResetCompletedEvent).",
                DebugUtility.Colors.Info);
        }

        private void Unregister()
        {
            if (!_registered) return;

            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_resetCompletedBinding);
            _registered = false;

            DebugUtility.LogVerbose<PhaseTransitionIntentWorldLifecycleBridge>(
                "[PhaseIntentBridge] Binding removido (WorldLifecycleResetCompletedEvent).",
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

            DebugUtility.Log<PhaseTransitionIntentWorldLifecycleBridge>(
                $"[PhaseIntentBridge] ResetCompleted -> consumindo intent e aplicando fase. signature='{signature}', phaseId='{intent.Plan.PhaseId}', reason='{Sanitize(intent.Reason)}'.",
                DebugUtility.Colors.Info);

            _phaseContext.SetPending(intent.Plan, intent.Reason);

            if (!_phaseContext.TryCommitPending(intent.Reason, out var committed))
            {
                DebugUtility.LogWarning<PhaseTransitionIntentWorldLifecycleBridge>(
                    $"[PhaseIntentBridge] Intent consumido, mas commit falhou (sem pending?). signature='{signature}', plan='{intent.Plan}'.");
                return;
            }

            // PhaseCommittedEvent é publicado pelo PhaseContextService.
            DebugUtility.LogVerbose<PhaseTransitionIntentWorldLifecycleBridge>(
                $"[PhaseIntentBridge] Commit aplicado. signature='{signature}', committed='{committed}'.");
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
