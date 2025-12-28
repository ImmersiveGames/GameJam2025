using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.
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
            var signature = evt.Context.ToString();

            EnsureHudService();

            _activeSignature = signature;

            _hudService?.Show(
                title: "Carregando...",
                details: null,
                progress01: 0.1f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] Started → Show. signature='{signature}'.");
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var signature = evt.Context.ToString();

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            EnsureHudService();

            _activeSignature = signature;

            _hudService?.Show(
                title: "Preparando...",
                details: null,
                progress01: 0.8f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] ScenesReady → Update. signature='{signature}'.");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            var signature = evt.Context.ToString();

            if (!EnsureActiveSignature(signature))
            {
                return;
            }

            EnsureHudService();

            _hudService?.Hide();

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] BeforeFadeOut → Hide. signature='{signature}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            var signature = evt.Context.ToString();

            if (!EnsureActiveSignature(signature, allowEmptyActive: true))
            {
                return;
            }

            EnsureHudService();

            _hudService?.Hide();
            _activeSignature = null;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] Completed → Safety hide. signature='{signature}'.");
        }

        private void EnsureHudService()
        {
            if (_hudService != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<INewScriptsLoadingHudService>(out var hudService) || hudService == null)
            {
                DebugUtility.LogVerbose<SceneFlowLoadingService>(
                    "[Loading] INewScriptsLoadingHudService indisponível. HUD permanecerá oculto.");
                return;
            }

            _hudService = hudService;
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
    }
}
