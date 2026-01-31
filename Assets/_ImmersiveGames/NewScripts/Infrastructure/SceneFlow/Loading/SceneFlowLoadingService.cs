using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.
    ///
    /// Opção A+:
    /// - Em transições com Fade: a HUD é "ensured" no Started, mas só fica visível após FadeIn concluir.
    /// - Em transições sem Fade: mantém o comportamento antigo (Show já no Started).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowLoadingService
    {

        private ILoadingHudService _hudService;

        private string _activeSignature;
        private string _pendingSignature;
        private string _pendingStep;
        private bool _pendingUseFade;
        private bool _warnedHudServiceMissingThisTransition;

        public SceneFlowLoadingService()
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

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] SceneFlowLoadingService registrado nos eventos de Scene Flow.");
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            ResetWarnings();

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Nova transição detectada. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = LoadingHudSteps.Started;
            _pendingUseFade = evt.Context.UseFade;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                _pendingUseFade ? $"[Loading] Started → Ensure only (Show após FadeIn). signature='{signature}'." : $"[Loading] Started → Ensure + Show (no-fade). signature='{signature}'.");

            _ = EnsureLoadedAsync(signature);

            // Transições sem fade mantêm o comportamento antigo.
            if (!_pendingUseFade)
            {
                TryShow(signature, LoadingHudSteps.Started, "started_no_fade");
            }
        }

        private void OnTransitionFadeInCompleted(SceneTransitionFadeInCompletedEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            _pendingSignature = signature;
            _pendingStep = LoadingHudSteps.AfterFadeIn;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] FadeInCompleted → Show. signature='{signature}'.");

            TryShow(signature, _pendingStep, "fade_in_completed");
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] ScenesReady com assinatura diferente. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStep = LoadingHudSteps.ScenesReady;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] ScenesReady → Update pending. signature='{signature}'.");

            // Para fade, isso atualiza a etapa se já estiver visível; para no-fade, mantém idempotente.
            TryShow(signature, _pendingStep, "scenes_ready");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            ClearPending();

            TryHide(signature, LoadingHudSteps.BeforeFadeOut, "before_fade_out");

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] BeforeFadeOut → Hide. signature='{signature}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            ClearPending();

            TryHide(signature, LoadingHudSteps.Completed, "completed");

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] Completed → Safety hide. signature='{signature}'.");
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

                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Evento ignorado (assinatura ativa vazia). incoming='{signature}'.");
                return false;
            }

            if (!string.Equals(_activeSignature, signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Evento ignorado (assinatura não corresponde). active='{_activeSignature}', incoming='{signature}'.");
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

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] HUD indisponível nesta transição. {context}.");
        }

        private async System.Threading.Tasks.Task EnsureLoadedAsync(string signature)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce("EnsureLoadedAsync não executado (serviço indisponível).");
                return;
            }

            await _hudService.EnsureLoadedAsync();

            // Se estamos em transição com Fade, só exibimos quando a etapa já avançou além do Started
            // (FadeInCompleted/ScenesReady).
            if (_pendingUseFade)
            {
                string step = ResolvePendingStep(signature, LoadingHudSteps.Started);
                if (!string.Equals(step, LoadingHudSteps.Started))
                {
                    TryShow(signature, step, "ensure_loaded_after_fade");
                }

                return;
            }

            // Em no-fade, o Show já acontece no Started (idempotente); nada extra aqui.
        }

        private void TryShow(string signature, string step, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Show pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            _hudService.Show(signature, step);
        }

        private void TryHide(string signature, string step, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Hide pendente ignorado. reason='{reason}', step='{step}'.");
                return;
            }

            _hudService.Hide(signature, step);
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

        private static class LoadingHudSteps
        {
            public const string Started = "Started";
            public const string AfterFadeIn = "AfterFadeIn";
            public const string ScenesReady = "ScenesReady";
            public const string BeforeFadeOut = "BeforeFadeOut";
            public const string Completed = "Completed";
        }
    }
}
