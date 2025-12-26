using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
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

        private ISceneLoadingHud _hud;
        private bool _isLoading;
        private string _lastContextSignature;
        private string _currentTitle;
        private string _currentDetails;
        private float _currentProgress01;

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

        public void AttachHud(ISceneLoadingHud hud)
        {
            if (hud == null)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    "[Loading] AttachHud chamado com referência nula. Ignorando.");
                return;
            }

            _hud = hud;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] HUD anexado ao SceneFlowLoadingService.");

            if (_isLoading)
            {
                ApplyHudState();
            }
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            _isLoading = true;
            _lastContextSignature = evt.Context.ToString();

            UpdateHudState(
                title: "Carregando...",
                details: null,
                progress01: 0.1f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionStarted → HUD loading ativo. signature='{_lastContextSignature}'.");
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (!_isLoading)
            {
                return;
            }

            UpdateHudState(
                title: "Preparando...",
                details: null,
                progress01: 0.8f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionScenesReady → HUD atualizado. signature='{_lastContextSignature}'.");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
            _currentProgress01 = 1f;
            HideHud();

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionBeforeFadeOut → HUD oculto. signature='{_lastContextSignature}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_isLoading && (_hud == null || !_hud.IsVisible))
            {
                return;
            }

            _isLoading = false;
            _currentProgress01 = 1f;
            HideHud();

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionCompleted → HUD forçado a ocultar. signature='{_lastContextSignature}'.");
        }

        private void UpdateHudState(string title, string details, float progress01)
        {
            _currentTitle = title;
            _currentDetails = details;
            _currentProgress01 = Clamp01(progress01);

            ApplyHudState();
        }

        private void ApplyHudState()
        {
            if (_hud == null)
            {
                return;
            }

            _hud.Show(_currentTitle, _currentDetails);
            _hud.SetProgress01(_currentProgress01);
        }

        private void HideHud()
        {
            if (_hud == null)
            {
                return;
            }

            _hud.Hide();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
