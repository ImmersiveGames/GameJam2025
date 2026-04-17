using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Loading.Runtime
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.
    ///
    /// Opcao A+:
    /// - Em transicoes com Fade: a HUD e "ensured" no Started, mas so fica visivel após FadeIn concluir.
    /// - Em transicoes sem Fade: mantem o comportamento antigo (Show já no Started).
    /// </summary>
        /// <summary>
    /// OWNER: orquestracao de visibilidade da HUD ao longo dos eventos do SceneFlow.
    /// NAO E OWNER: carregamento tecnico da cena HUD (delegado ao LoadingHudService).
    /// PUBLISH/CONSUME: consome Started/FadeInCompleted/ScenesReady/BeforeFadeOut/Completed; nao publica eventos.
    /// Fases tocadas: TransitionStarted, FadeIn completed, ScenesReady, BeforeFadeOut, TransitionCompleted.
    /// </summary>
[DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudOrchestrator : IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionFadeInCompletedEvent> _transitionFadeInCompletedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _transitionBeforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private ILoadingHudService _hudService;
        private ILoadingPresentationService _presentationService;

        private string _activeSignature;
        private string _pendingSignature;
        private string _pendingStep;
        private bool _pendingUseFade;
        private bool _warnedHudServiceMissingThisTransition;
        private bool _hudVisible;
        private string _visibleSignature;
        private string _visibleStep;
        private bool _isRegistered;
        private bool _disposed;

        public LoadingHudOrchestrator()
        {
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionFadeInCompletedBinding = new EventBinding<SceneTransitionFadeInCompletedEvent>(OnTransitionFadeInCompleted);
            _transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnTransitionScenesReady);
            _transitionBeforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnTransitionBeforeFadeOut);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EnsureRegistered();
        }

        public void EnsureRegistered()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LoadingHudOrchestrator));
            }

            if (_isRegistered)
            {
                return;
            }

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionFadeInCompletedEvent>.Register(_transitionFadeInCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_transitionScenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_transitionBeforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            _isRegistered = true;

            DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                "[Loading] LoadingHudOrchestrator registrado nos eventos de Scene Flow.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_isRegistered)
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
                EventBus<SceneTransitionFadeInCompletedEvent>.Unregister(_transitionFadeInCompletedBinding);
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(_transitionScenesReadyBinding);
                EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_transitionBeforeFadeOutBinding);
                EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
                _isRegistered = false;

                DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                    "[Loading] LoadingHudOrchestrator desregistrado dos eventos de Scene Flow.",
                    DebugUtility.Colors.Info);
            }

            _disposed = true;
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);

            ResetWarnings();

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] Nova transicao detectada. Substituindo pendencias. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = SceneFlowLoadingPhases.Started;
            _pendingUseFade = evt.context.UseFade;

            DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                $"[LoadingStart] signature='{signature}' useFade={_pendingUseFade}.",
                DebugUtility.Colors.Info);

            if (_pendingUseFade)
            {
                _ = EnsureLoadedAsync(signature);
            }

            // Transicoes sem fade mantem o comportamento antigo.
            if (!_pendingUseFade)
            {
                _ = EnsureThenShowAsync(signature, SceneFlowLoadingPhases.Started, "started_no_fade");
            }
        }

        private void OnTransitionFadeInCompleted(SceneTransitionFadeInCompletedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            _pendingSignature = signature;
            _pendingStep = SceneFlowLoadingPhases.AfterFadeIn;


            _ = EnsureThenShowAsync(signature, _pendingStep, "fade_in_completed");
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] ScenesReady com assinatura diferente. Substituindo pendencias. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = SceneFlowLoadingPhases.ScenesReady;
            _pendingUseFade = evt.context.UseFade;


            // Para fade/no-fade: Show e idempotente por assinatura quando a HUD ja estiver visivel.
            _ = EnsureThenShowAsync(signature, _pendingStep, "scenes_ready");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            ClearPending();

            TryHide(signature, SceneFlowLoadingPhases.BeforeFadeOut, "before_fade_out");

        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            ClearPending();

            if (TryResolvePresentationService())
            {
                _presentationService.SetProgress(signature, new LoadingProgressSnapshot(1f, "Ready", evt.context.Reason));
            }

            TryHide(signature, SceneFlowLoadingPhases.Completed, "completed");

            DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                $"[LoadingCompleted] signature='{signature}'.",
                DebugUtility.Colors.Info);

        }

        private bool IsSignatureMismatch(string signature)
        {
            if (string.IsNullOrWhiteSpace(_pendingSignature))
            {
                return false;
            }

            return !string.Equals(_pendingSignature, signature);
        }

        private bool EnsureActiveSignature(string signature, bool allowEmptyActive = false)
        {
            if (string.IsNullOrWhiteSpace(_activeSignature))
            {
                if (allowEmptyActive)
                {
                    _activeSignature = signature;
                    return true;
                }

                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] Evento ignorado (assinatura ativa vazia). incoming='{signature}'.");
                return false;
            }

            if (!string.Equals(_activeSignature, signature))
            {
                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] Evento ignorado (assinatura nao corresponde). active='{_activeSignature}', incoming='{signature}'.");
                return false;
            }

            return true;
        }

        private void ClearPending()
        {
            _pendingSignature = null;
            _pendingStep = null;
            _activeSignature = null;
            _pendingUseFade = false;
        }

        private void ResetWarnings()
        {
            _warnedHudServiceMissingThisTransition = false;
        }

        private void WarnHudMissingOnce(string context)
        {
            if (_warnedHudServiceMissingThisTransition)
            {
                return;
            }

            _warnedHudServiceMissingThisTransition = true;

            DebugUtility.LogWarning<LoadingHudOrchestrator>(
                $"[LoadingDegraded] HUD indisponivel nesta transicao. {context}.");
        }

        private async System.Threading.Tasks.Task EnsureLoadedAsync(string signature)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce("EnsureLoadedAsync nao executado (servico indisponivel).");
                return;
            }

            try
            {
                await _hudService.EnsureLoadedAsync(signature);
            }
            catch (Exception ex)
            {
                WarnHudMissingOnce($"EnsureLoadedAsync falhou ({ex.GetType().Name}). {ex.Message}");
                return;
            }

            // Se estamos em transicao com Fade, so exibimos quando a etapa já avancou além do Started
            // (FadeInCompleted/ScenesReady).
            if (_pendingUseFade)
            {
                string step = ResolvePendingStep(signature, SceneFlowLoadingPhases.Started);
                if (!string.Equals(step, SceneFlowLoadingPhases.Started))
                {
                    TryShow(signature, step, "ensure_loaded_after_fade");
                }

            }

            // Em no-fade, o Show já acontece no Started (idempotente); nada extra aqui.
        }

        private async System.Threading.Tasks.Task EnsureThenShowAsync(string signature, string step, string reason)
        {
            if (_hudVisible && string.Equals(_visibleSignature, signature))
            {
                string visiblePhase = string.IsNullOrWhiteSpace(_visibleStep) ? "(null)" : _visibleStep;
                DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                    $"[LoadingHudShow] skipped (already visible). signature='{signature}' requestedPhase='{step}' visiblePhase='{visiblePhase}'.");
                return;
            }

            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Show pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            try
            {
                await _hudService.EnsureLoadedAsync(signature);
            }
            catch (Exception ex)
            {
                WarnHudMissingOnce($"EnsureLoadedAsync falhou ({ex.GetType().Name}). {ex.Message}");
                return;
            }

            TryShow(signature, step, reason);
        }

        private void TryShow(string signature, string step, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Show pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            if (_hudVisible && string.Equals(_visibleSignature, signature))
            {
                DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                    $"[LoadingHudShow] skipped (already visible). signature='{signature}' requestedPhase='{step}' visiblePhase='{_visibleStep}'.");
                return;
            }

            _hudService.Show(signature, step);

            _hudVisible = true;
            _visibleSignature = signature;
            _visibleStep = step;
        }

        private void TryHide(string signature, string step, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Hide pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            if (!_hudVisible)
            {
                return;
            }

            _hudService.Hide(signature, step);

            _hudVisible = false;
            _visibleSignature = null;
            _visibleStep = null;
        }

        private bool TryResolveHudService()
        {
            if (_hudService != null)
            {
                return true;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILoadingHudService>(out var service) || service == null)
            {
                return false;
            }

            _hudService = service;
            return true;
        }

        private bool TryResolvePresentationService()
        {
            if (_presentationService != null)
            {
                return true;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILoadingPresentationService>(out var service) || service == null)
            {
                return false;
            }

            _presentationService = service;
            return true;
        }

        private string ResolvePendingStep(string signature, string fallback)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return fallback;
            }

            if (string.Equals(signature, _pendingSignature))
            {
                return string.IsNullOrWhiteSpace(_pendingStep) ? fallback : _pendingStep;
            }

            return fallback;
        }
    }
}



