#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using _ImmersiveGames.NewScripts.Core.DI;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelStartCommitBridge : IDisposable
    {
        private const string UnknownSignature = "<none>";
        private const string LevelChangePrefix = "LevelChange/";
        private const string QaLevelPrefix = "QA/Levels/";

        // Se o Completed chegar antes do Release do token, aguardamos alguns frames.
        // A intenção é cobrir ordem de handlers sem depender de ordering global do EventBus.
        private const int CompletedGateWaitTimeoutMs = 1500;

        private readonly EventBinding<ContentSwapCommittedEvent> _committedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private LevelStartRequest? _pendingRequest;

        // Importante:
        // _pendingSignature deve conter SOMENTE assinatura verificável.
        // Se a assinatura for desconhecida ("<none>"), mantemos vazio para não descartar por mismatch.
        private string _pendingSignature = string.Empty;
        private string _lastCommitSignature = string.Empty;
        private string _lastCommitReason = string.Empty;

        // Guard: evita executar o pipeline pendente duas vezes se houver reentrância/dupla notificação.
        private int _pendingRunInProgress;

        private bool _disposed;

        public LevelStartCommitBridge()
        {
            _committedBinding = new EventBinding<ContentSwapCommittedEvent>(OnContentSwapCommitted);
            EventBus<ContentSwapCommittedEvent>.Register(_committedBinding);

            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            DebugUtility.LogVerbose<LevelStartCommitBridge>(
                "[LevelStart] Bridge registrado (ContentSwapCommittedEvent -> LevelChange/IntroStageController pipeline).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            EventBus<ContentSwapCommittedEvent>.Unregister(_committedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);

            _pendingRequest = null;
            _pendingSignature = string.Empty;
        }

        /// <summary>
        /// Indica se existe um pipeline pendente para esta assinatura de transição.
        /// Usado para evitar disparo duplicado de IntroStageController (SceneFlow/Completed + ContentSwapCommitted/pendente).
        /// </summary>
        public bool HasPendingFor(string contextSignature)
        {
            if (_disposed || _pendingRequest == null)
            {
                return false;
            }

            string verifiableSig = NormalizeVerifiableSignature(contextSignature);

            if (verifiableSig.Length == 0 || _pendingSignature.Length == 0)
            {
                return false;
            }

            return string.Equals(_pendingSignature, verifiableSig, StringComparison.Ordinal);
        }

        /// <summary>
        /// Indica se o pipeline pendente deve suprimir a IntroStageController para esta assinatura concluída.
        /// </summary>
        public bool ShouldSuppressIntroStage(string completedSignature)
        {
            if (_disposed || _pendingRequest == null)
            {
                return false;
            }

            if (_pendingSignature.Length == 0)
            {
                return true;
            }

            string verifiableSig = NormalizeVerifiableSignature(completedSignature);
            if (verifiableSig.Length == 0)
            {
                return false;
            }

            return string.Equals(_pendingSignature, verifiableSig, StringComparison.Ordinal);
        }

        /// <summary>
        /// Indica se a assinatura concluída pertence a um ContentSwap (não-Levels).
        /// Usado para evitar IntroStageController no fluxo de ContentSwap com SceneFlow.
        /// </summary>
        public bool IsContentSwapSignature(string completedSignature)
        {
            if (_disposed)
            {
                return false;
            }

            string verifiableSig = NormalizeVerifiableSignature(completedSignature);
            if (verifiableSig.Length == 0 || _lastCommitSignature.Length == 0)
            {
                return false;
            }

            if (!string.Equals(_lastCommitSignature, verifiableSig, StringComparison.Ordinal))
            {
                return false;
            }

            return IsContentSwapReason(_lastCommitReason);
        }

        private async void OnContentSwapCommitted(ContentSwapCommittedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.Current.IsValid)
            {
                DebugUtility.LogWarning<LevelStartCommitBridge>(
                    "[LevelStart] ContentSwapCommittedEvent com Current inválido. Pipeline ignorado.");
                return;
            }

            string reason = string.IsNullOrWhiteSpace(evt.Reason) ? "ContentSwap/Committed" : evt.Reason.Trim();
            string signature = ResolveContextSignature(out string targetScene);
            CacheLastCommit(signature, reason);

            if (!ShouldHandleLevelChange(reason))
            {
                DebugUtility.LogVerbose<LevelStartCommitBridge>(
                    $"[LevelStart] ContentSwapCommittedEvent ignorado (não é LevelChange). reason='{reason}'.");
                return;
            }

            string contentSwapStartReason = $"LevelStart/Committed|contentId={evt.Current.ContentId}|reason={reason}";

            if (!IsGameplaySceneOrTarget(targetScene))
            {
                DebugUtility.LogVerbose<LevelStartCommitBridge>(
                    "[LevelStart] ContentSwapCommittedEvent fora da gameplay. Pipeline ignorado.");
                return;
            }

            var request = new LevelStartRequest(
                signature,
                evt.Current.ContentId,
                targetScene,
                contentSwapStartReason);

            if (IsSceneTransitionGateActive())
            {
                string verifiableSig = NormalizeVerifiableSignature(signature);

                if (_pendingRequest != null)
                {
                    DebugUtility.LogWarning<LevelStartCommitBridge>(
                        $"[LevelStart] Pipeline pendente substituído por novo commit. oldSig='{(_pendingSignature.Length == 0 ? UnknownSignature : _pendingSignature)}', newSig='{(verifiableSig.Length == 0 ? UnknownSignature : verifiableSig)}'.");
                }

                _pendingRequest = request;
                _pendingSignature = verifiableSig;

                DebugUtility.LogVerbose<LevelStartCommitBridge>(
                    $"[LevelStart] SceneTransition ativo; pipeline pendente até TransitionCompleted. signature='{(verifiableSig.Length == 0 ? UnknownSignature : verifiableSig)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            await LevelStartPipeline.RunAsync(request);
        }

        private async void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed || _pendingRequest == null)
            {
                return;
            }

            // Guard: evita executar duas vezes em cenários raros (reentrância/duplicidade).
            if (Interlocked.CompareExchange(ref _pendingRunInProgress, 1, 0) == 1)
            {
                return;
            }

            try
            {
                // Se o Completed chegar antes do Release do token (ordem de handlers), aguardamos alguns frames.
                // Isso evita o bug: "mantém pendente e nunca roda" por não existir segundo Completed.
                if (IsSceneTransitionGateActive())
                {
                    string? completedSignatureNow = SceneTransitionSignatureUtil.Compute(evt.Context);

                    DebugUtility.LogWarning<LevelStartCommitBridge>(
                        $"[LevelStart] TransitionCompleted observado, mas SceneTransition token ainda está ativo (provável ordering). " +
                        $"Aguardando liberação para executar pipeline pendente. completedSig='{completedSignatureNow}'.");

                    int startTick = Environment.TickCount;

                    while (!_disposed && _pendingRequest != null && IsSceneTransitionGateActive())
                    {
                        await Task.Yield();

                        int dt = unchecked(Environment.TickCount - startTick);
                        if (dt >= CompletedGateWaitTimeoutMs)
                        {
                            DebugUtility.LogWarning<LevelStartCommitBridge>(
                                $"[LevelStart] Timeout aguardando liberação do SceneTransition token após Completed. " +
                                $"Mantendo pendência. timeoutMs={CompletedGateWaitTimeoutMs}.");
                            return; // mantém pendente (fallback seguro)
                        }
                    }

                    if (_disposed || _pendingRequest == null)
                    {
                        return;
                    }
                }

                // Se não temos assinatura verificável, NUNCA descartamos por mismatch.
                if (HasVerifiableSignature(_pendingSignature))
                {
                    string? completedSignature = SceneTransitionSignatureUtil.Compute(evt.Context);
                    if (!string.Equals(completedSignature, _pendingSignature, StringComparison.Ordinal))
                    {
                        DebugUtility.LogWarning<LevelStartCommitBridge>(
                            $"[LevelStart] TransitionCompleted com assinatura divergente; descartando pendência. expected='{_pendingSignature}', got='{completedSignature}'.");
                        _pendingRequest = null;
                        _pendingSignature = string.Empty;
                        return;
                    }

                    DebugUtility.LogVerbose<LevelStartCommitBridge>(
                        $"[LevelStart] TransitionCompleted observado; executando pipeline pendente. signature='{completedSignature}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    DebugUtility.LogVerbose<LevelStartCommitBridge>(
                        "[LevelStart] TransitionCompleted observado; assinatura pendente não-verificável ('<none>'). Executando pipeline pendente sem validação de mismatch.",
                        DebugUtility.Colors.Info);
                }

                var request = _pendingRequest.Value;
                _pendingRequest = null;
                _pendingSignature = string.Empty;

                await LevelStartPipeline.RunAsync(request);
            }
            finally
            {
                Interlocked.Exchange(ref _pendingRunInProgress, 0);
            }
        }

        private static bool ShouldHandleLevelChange(string reason)
        {
            return reason.StartsWith(LevelChangePrefix, StringComparison.Ordinal)
                   || reason.Contains($"|reason={LevelChangePrefix}", StringComparison.Ordinal)
                   || reason.StartsWith(QaLevelPrefix, StringComparison.Ordinal)
                   || reason.Contains($"|reason={QaLevelPrefix}", StringComparison.Ordinal);
        }

        private static bool IsContentSwapReason(string reason)
        {
            return reason.StartsWith("ContentSwap/", StringComparison.Ordinal)
                   || reason.StartsWith("QA/ContentSwap/", StringComparison.Ordinal)
                   || reason.Contains("|reason=ContentSwap/", StringComparison.Ordinal)
                   || reason.Contains("|reason=QA/ContentSwap/", StringComparison.Ordinal);
        }

        private void CacheLastCommit(string signature, string reason)
        {
            _lastCommitSignature = NormalizeVerifiableSignature(signature);
            _lastCommitReason = reason ?? string.Empty;
        }

        private static string ResolveContextSignature(out string targetScene)
        {
            targetScene = SceneManager.GetActiveScene().name;

            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) &&
                cache != null &&
                cache.TryGetLast(out string? signature, out _, out string? cachedTarget))
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

            return SceneManager.GetActiveScene().name == "GameplayScene";
        }

        private static bool IsGameplaySceneOrTarget(string targetScene)
        {
            // Nota: IGameplaySceneClassifier só consegue classificar a cena ATIVA (marker em runtime).
            // Para cenas-alvo ainda não carregadas, usamos fallback por nome.
            if (IsGameplayScene())
            {
                return true;
            }

            return targetScene == "GameplayScene";
        }

        private static string NormalizeVerifiableSignature(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature) || signature == UnknownSignature)
            {
                return string.Empty;
            }

            return signature.Trim();
        }

        private static bool HasVerifiableSignature(string signature)
        {
            return !string.IsNullOrWhiteSpace(signature) && signature != UnknownSignature;
        }
    }
}
