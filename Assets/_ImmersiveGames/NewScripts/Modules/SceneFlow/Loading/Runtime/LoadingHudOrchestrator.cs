using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.
    ///
    /// Opcao A+:
    /// - Em transicoes com Fade: a HUD e "ensured" no Started, mas so fica visivel após FadeIn concluir.
    /// - Em transicoes sem Fade: mantem o comportamento antigo (Show já no Started).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudOrchestrator
    {

        private ILoadingHudService _hudService;

        private string _activeSignature;
        private string _pendingSignature;
        private string _pendingStep;
        private bool _pendingUseFade;
        private bool _warnedHudServiceMissingThisTransition;
        private bool _hudVisible;
        private string _visibleSignature;
        private string _visibleStep;

        public LoadingHudOrchestrator()
        {
            var transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            var transitionFadeInCompletedBinding = new EventBinding<SceneTransitionFadeInCompletedEvent>(OnTransitionFadeInCompleted);
            var transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnTransitionScenesReady);
            var transitionBeforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnTransitionBeforeFadeOut);
            var transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(transitionStartedBinding);
            EventBus<SceneTransitionFadeInCompletedEvent>.Register(transitionFadeInCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(transitionScenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(transitionBeforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(transitionCompletedBinding);

            DebugUtility.LogVerbose<LoadingHudOrchestrator>(
                "[Loading] LoadingHudOrchestrator registrado nos eventos de Scene Flow.");
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.Context);

            ResetWarnings();

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] Nova transicao detectada. Substituindo pendencias. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = SceneFlowLoadingPhases.Started;
            _pendingUseFade = evt.Context.UseFade;

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
            string signature = SceneTransitionSignature.Compute(evt.Context);

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
            string signature = SceneTransitionSignature.Compute(evt.Context);

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<LoadingHudOrchestrator>(
                    $"[Loading] ScenesReady com assinatura diferente. Substituindo pendencias. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = SceneFlowLoadingPhases.ScenesReady;
            _pendingUseFade = evt.Context.UseFade;


            // Para fade, isso atualiza a etapa se já estiver visivel; para no-fade, mantem idempotente.
            _ = EnsureThenShowAsync(signature, _pendingStep, "scenes_ready");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.Context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            ClearPending();

            TryHide(signature, SceneFlowLoadingPhases.BeforeFadeOut, "before_fade_out");

        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.Context);

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            ClearPending();

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
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Show pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            if (_hudVisible && string.Equals(_visibleSignature, signature))
            {
                if (string.Equals(_visibleStep, step))
                {
                    return;
                }
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
                if (string.Equals(_visibleStep, step))
                {
                    return;
                }
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
