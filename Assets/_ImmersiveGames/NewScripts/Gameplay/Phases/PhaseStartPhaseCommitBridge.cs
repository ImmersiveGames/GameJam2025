#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseStartPhaseCommitBridge : IDisposable
    {
        private readonly EventBinding<PhaseCommittedEvent> _committedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private PhaseStartRequest? _pendingRequest;
        private string _pendingSignature;
        private bool _disposed;

        public PhaseStartPhaseCommitBridge()
        {
            _committedBinding = new EventBinding<PhaseCommittedEvent>(OnPhaseCommitted);
            EventBus<PhaseCommittedEvent>.Register(_committedBinding);

            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                "[PhaseStart] Bridge registrado (PhaseCommittedEvent -> Pregame pipeline).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseCommittedEvent>.Unregister(_committedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
        }

        private async void OnPhaseCommitted(PhaseCommittedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.Current.IsValid)
            {
                DebugUtility.LogWarning<PhaseStartPhaseCommitBridge>(
                    "[PhaseStart] PhaseCommittedEvent com Current inválido. Pipeline ignorado.");
                return;
            }

            if (!IsGameplayScene())
            {
                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    "[PhaseStart] PhaseCommittedEvent fora da gameplay. Pipeline ignorado.");
                return;
            }

            var reason = string.IsNullOrWhiteSpace(evt.Reason) ? "PhaseCommitted" : evt.Reason.Trim();
            var signature = ResolveContextSignature(out var targetScene);
            var phaseStartReason = $"PhaseStart/Committed phaseId='{evt.Current.PhaseId}' reason='{reason}'";

            var request = new PhaseStartRequest(
                contextSignature: signature,
                phaseId: evt.Current.PhaseId,
                targetScene: targetScene,
                reason: phaseStartReason);

            if (IsSceneTransitionGateActive())
            {
                _pendingRequest = request;
                _pendingSignature = signature;

                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    $"[PhaseStart] SceneTransition ativo; pipeline pendente até TransitionCompleted. signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            await PhaseStartPipeline.RunAsync(request);
        }

        private async void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed || _pendingRequest == null)
            {
                return;
            }

            var signature = SceneTransitionSignatureUtil.Compute(evt.Context);
            if (!string.IsNullOrEmpty(_pendingSignature) &&
                !string.Equals(signature, _pendingSignature, StringComparison.Ordinal))
            {
                return;
            }

            var request = _pendingRequest.Value;
            _pendingRequest = null;
            _pendingSignature = string.Empty;

            DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                $"[PhaseStart] TransitionCompleted observado; executando pipeline pendente. signature='{signature}'.",
                DebugUtility.Colors.Info);

            await PhaseStartPipeline.RunAsync(request);
        }

        private static string ResolveContextSignature(out string targetScene)
        {
            targetScene = SceneManager.GetActiveScene().name;

            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) &&
                cache != null &&
                cache.TryGetLast(out var signature, out _, out var cachedTarget))
            {
                if (!string.IsNullOrWhiteSpace(cachedTarget))
                {
                    targetScene = cachedTarget;
                }

                return string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();
            }

            return "<none>";
        }

        private static bool IsSceneTransitionGateActive()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                return false;
            }

            return gate.IsTokenActive(SimulationGateTokens.SceneTransition);
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) &&
                classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return string.Equals(SceneManager.GetActiveScene().name, "GameplayScene", StringComparison.Ordinal);
        }
    }
}
