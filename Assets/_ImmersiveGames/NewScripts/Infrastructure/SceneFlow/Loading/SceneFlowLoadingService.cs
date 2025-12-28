using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.asd
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowLoadingService
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _transitionBeforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private INewScriptsLoadingHudService _hudService;
        private string _activeSignature;
        private string _pendingSignature;
        private string _pendingPhase;
        private bool _warnedHudServiceMissingThisTransition;

        public SceneFlowLoadingService()
        {
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnTransitionScenesReady);
            _transitionBeforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnTransitionBeforeFadeOut);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_transitionScenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_transitionBeforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] SceneFlowLoadingService registrado nos eventos de Scene Flow.");
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            var signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            ResetWarnings();

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Nova transição detectada. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingPhase = LoadingHudPhases.Started;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] Started → Ensure + Show. signature='{signature}'.");

            _ = EnsureLoadedAndShowAsync(signature);
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] ScenesReady com assinatura diferente. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingPhase = LoadingHudPhases.ScenesReady;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] ScenesReady → Update pending. signature='{signature}'.");

            TryShow(signature, _pendingPhase, "scenes_ready");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            var signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            ClearPending();

            TryHide(signature, LoadingHudPhases.BeforeFadeOut, "before_fade_out");

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] BeforeFadeOut → Hide. signature='{signature}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            var signature = SceneTransitionSignatureUtil.Compute(evt.Context);

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            ClearPending();

            TryHide(signature, LoadingHudPhases.Completed, "completed");

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
            _pendingPhase = null;
            _activeSignature = null;
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

        private async System.Threading.Tasks.Task EnsureLoadedAndShowAsync(string signature)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce("EnsureLoadedAsync não executado (serviço indisponível).");
                return;
            }

            await _hudService.EnsureLoadedAsync();

            var phase = ResolvePendingPhase(signature, LoadingHudPhases.Started);
            TryShow(signature, phase, "ensure_loaded");
        }

        private void TryShow(string signature, string phase, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Show pendente ignorado. reason='{reason}', phase='{phase}'.");
                return;
            }

            _hudService.Show(signature, phase);
        }

        private void TryHide(string signature, string phase, string reason)
        {
            if (!TryResolveHudService())
            {
                WarnHudMissingOnce($"Hide pendente ignorado. reason='{reason}', phase='{phase}'.");
                return;
            }

            _hudService.Hide(signature, phase);
        }

        private bool TryResolveHudService()
        {
            if (_hudService != null)
            {
                return true;
            }

            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var service) || service == null)
            {
                return false;
            }

            _hudService = service;
            return true;
        }

        private string ResolvePendingPhase(string signature, string fallback)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return fallback;
            }

            if (string.Equals(signature, _pendingSignature))
            {
                return string.IsNullOrWhiteSpace(_pendingPhase) ? fallback : _pendingPhase;
            }

            return fallback;
        }

        private static class LoadingHudPhases
        {
            public const string Started = "Started";
            public const string ScenesReady = "ScenesReady";
            public const string BeforeFadeOut = "BeforeFadeOut";
            public const string Completed = "Completed";
        }
    }
}
