#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseStartPhaseCommitBridge : IDisposable
    {
        private const string UnknownSignature = "<none>";

        private readonly EventBinding<PhaseCommittedEvent> _committedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private PhaseStartRequest? _pendingRequest;

        // Importante:
        // _pendingSignature deve conter SOMENTE assinatura verificável.
        // Se a assinatura for desconhecida ("<none>"), mantemos vazio para não descartar por mismatch.
        private string _pendingSignature = string.Empty;

        private bool _disposed;

        public PhaseStartPhaseCommitBridge()
        {
            _committedBinding = new EventBinding<PhaseCommittedEvent>(OnPhaseCommitted);
            EventBus<PhaseCommittedEvent>.Register(_committedBinding);

            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                "[PhaseStart] Bridge registrado (PhaseCommittedEvent -> IntroStage pipeline).",
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

            _pendingRequest = null;
            _pendingSignature = string.Empty;
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

            var reason = string.IsNullOrWhiteSpace(evt.Reason) ? "PhaseCommitted" : evt.Reason.Trim();
            var signature = ResolveContextSignature(out var targetScene);
            var phaseStartReason = $"PhaseStart/Committed phaseId='{evt.Current.PhaseId}' reason='{reason}'";

            if (!IsGameplaySceneOrTarget(targetScene))
            {
                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    "[PhaseStart] PhaseCommittedEvent fora da gameplay. Pipeline ignorado.");
                return;
            }

            var request = new PhaseStartRequest(
                contextSignature: signature,
                phaseId: evt.Current.PhaseId,
                targetScene: targetScene,
                reason: phaseStartReason);

            if (IsSceneTransitionGateActive())
            {
                var verifiableSig = NormalizeVerifiableSignature(signature);

                if (_pendingRequest != null)
                {
                    DebugUtility.LogWarning<PhaseStartPhaseCommitBridge>(
                        $"[PhaseStart] Pipeline pendente substituído por novo commit. oldSig='{(_pendingSignature.Length == 0 ? UnknownSignature : _pendingSignature)}', newSig='{(verifiableSig.Length == 0 ? UnknownSignature : verifiableSig)}'.");
                }

                _pendingRequest = request;
                _pendingSignature = verifiableSig;

                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    $"[PhaseStart] SceneTransition ativo; pipeline pendente até TransitionCompleted. signature='{(verifiableSig.Length == 0 ? UnknownSignature : verifiableSig)}'.",
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

            // Se não temos assinatura verificável, NUNCA descartamos por mismatch.
            if (HasVerifiableSignature(_pendingSignature))
            {
                var completedSignature = SceneTransitionSignatureUtil.Compute(evt.Context);
                if (!string.Equals(completedSignature, _pendingSignature, StringComparison.Ordinal))
                {
                    DebugUtility.LogWarning<PhaseStartPhaseCommitBridge>(
                        $"[PhaseStart] TransitionCompleted com assinatura divergente; descartando pendência. expected='{_pendingSignature}', got='{completedSignature}'.");
                    _pendingRequest = null;
                    _pendingSignature = string.Empty;
                    return;
                }

                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    $"[PhaseStart] TransitionCompleted observado; executando pipeline pendente. signature='{completedSignature}'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogVerbose<PhaseStartPhaseCommitBridge>(
                    "[PhaseStart] TransitionCompleted observado; assinatura pendente não-verificável ('<none>'). Executando pipeline pendente sem validação de mismatch.",
                    DebugUtility.Colors.Info);
            }

            var request = _pendingRequest.Value;
            _pendingRequest = null;
            _pendingSignature = string.Empty;

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

                return string.IsNullOrWhiteSpace(signature) ? UnknownSignature : signature.Trim();
            }

            return UnknownSignature;
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

        private static bool IsGameplaySceneOrTarget(string targetScene)
        {
            if (IsGameplayScene())
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                return false;
            }

            return string.Equals(targetScene.Trim(), GameNavigationCatalog.SceneGameplay, StringComparison.Ordinal);
        }

        private static bool HasVerifiableSignature(string? signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            return !string.Equals(signature.Trim(), UnknownSignature, StringComparison.Ordinal);
        }

        private static string NormalizeVerifiableSignature(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return string.Empty;
            }

            var trimmed = signature.Trim();
            if (string.Equals(trimmed, UnknownSignature, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return trimmed;
        }
    }
}
