using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation.Bindings
{
    /// <summary>
    /// Contexto visual local de PostRunMenu.
    ///
    /// Consome a projeção canônica de resultado do PostRun e emite intents downstream.
    /// Gate, ownership e projeção de resultado ficam fora da camada visual.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed partial class PostRunOverlayController : MonoBehaviour
    {
        private const string RestartReason = "PostRunMenu/Restart";
        private const string ExitToMenuReason = "PostRunMenu/ExitToMenu";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitToMenuButton;

        [Inject] private IPostRunOwnershipService _postGameOwnership;
        [Inject] private IPostRunResultService _postGameResultService;
        [Inject] private IPostLevelActionsService _postLevelActionsService;

        private bool _dependenciesInjected;
        private EventBinding<PostRunEnteredEvent> _postGameEnteredBinding;
        private EventBinding<PostRunExitedEvent> _postGameExitedBinding;
        private EventBinding<PostRunResultUpdatedEvent> _postGameResultUpdatedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private bool _isVisible;
        private bool _actionRequested;

        private void Awake()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            _postGameEnteredBinding = new EventBinding<PostRunEnteredEvent>(OnPostRunEntered);
            _postGameExitedBinding = new EventBinding<PostRunExitedEvent>(OnPostRunExited);
            _postGameResultUpdatedBinding = new EventBinding<PostRunResultUpdatedEvent>(OnPostRunResultUpdated);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            RegisterBindings();
            HideImmediate();
            ValidateReferences();
        }

        private void Start()
        {
            EnsureDependenciesInjected();

            if (ShouldShowOverlayOnStart())
            {
                ApplyStatus(ResolveRequiredResultService("Start"));
                Show();
            }
        }

        private void OnEnable() => RegisterBindings();

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void OnDestroy()
        {
            UnregisterBindings();
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botao de restart.
        /// </summary>
        public void OnClickRestart()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostRunOverlayController>(
                    "[PostRunMenu] Restart ignorado (acao ja solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu][Intent] Restart solicitado. Delegando downstream via IPostLevelActionsService.",
                DebugUtility.Colors.Info);

            ClosePostRun("Playing", RestartReason);
            HideImmediate();

            _ = ExecutePostLevelActionAsync(
                actionName: "RestartLevel",
                reason: RestartReason,
                action: ct => _postLevelActionsService.RestartLevelAsync(RestartReason, ct));
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botao de voltar ao menu.
        /// </summary>
        public void OnClickExitToMenu()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostRunOverlayController>(
                    "[PostRunMenu] ExitToMenu ignorado (acao ja solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu][Intent] ExitToMenu solicitado. Delegando downstream via IPostLevelActionsService.",
                DebugUtility.Colors.Info);

            ClosePostRun("FrontendMenu", ExitToMenuReason);
            HideImmediate();

            _ = ExecutePostLevelActionAsync(
                actionName: "ExitToMenu",
                reason: ExitToMenuReason,
                action: ct => _postLevelActionsService.ExitToMenuAsync(ExitToMenuReason, ct));
        }

        private async Task ExecutePostLevelActionAsync(string actionName, string reason, Func<CancellationToken, Task> action)
        {
            if (_postLevelActionsService == null)
            {
                HardFailFastH1.Trigger(typeof(PostRunOverlayController),
                    $"[FATAL][H1][PostRun] IPostLevelActionsService indisponivel. action='{actionName}' reason='{reason}'.");
                return;
            }

            try
            {
                DebugUtility.Log<PostRunOverlayController>(
                    $"[PostRunMenu][Delegate] {actionName} entregue ao executor real IPostLevelActionsService. reason='{reason}'.");
                await action(CancellationToken.None);
                DebugUtility.Log<PostRunOverlayController>(
                    $"[PostRunMenu][Execute] {actionName} executado downstream. reason='{reason}'.");
            }
            catch (Exception ex)
            {
                _actionRequested = false;
                DebugUtility.LogWarning<PostRunOverlayController>(
                    $"[PostRunMenu] {actionName} falhou. reason='{reason}', notes='{ex.GetType().Name}'.");
            }
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<PostRunEnteredEvent>.Register(_postGameEnteredBinding);
            EventBus<PostRunExitedEvent>.Register(_postGameExitedBinding);
            EventBus<PostRunResultUpdatedEvent>.Register(_postGameResultUpdatedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu] Bindings de PostRunEntered/PostRunExited/PostRunResultUpdated/GameRunStarted registrados.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<PostRunEnteredEvent>.Unregister(_postGameEnteredBinding);
            EventBus<PostRunExitedEvent>.Unregister(_postGameExitedBinding);
            EventBus<PostRunResultUpdatedEvent>.Unregister(_postGameResultUpdatedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;

            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu] Bindings de PostRunEntered/PostRunExited/PostRunResultUpdated/GameRunStarted removidos.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu] GameRunStartedEvent recebido. Ocultando contexto visual local.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
        }

        private void OnPostRunEntered(PostRunEnteredEvent evt)
        {
            if (_actionRequested || _isVisible)
            {
                return;
            }

            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu] PostRunMenuEnteredEvent recebido. Exibindo contexto visual local.",
                DebugUtility.Colors.Info);

            ApplyStatus(ResolveRequiredResultService("OnPostRunEntered"));
            Show();
        }

        private void OnPostRunExited(PostRunExitedEvent evt)
        {
            DebugUtility.LogVerbose<PostRunOverlayController>(
                "[PostRunMenu] PostRunMenuExitedEvent recebido. Ocultando contexto visual.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
        }

        private void OnPostRunResultUpdated(PostRunResultUpdatedEvent evt)
        {
            if (!_isVisible)
            {
                return;
            }

            ApplyStatus(ResolveRequiredResultService("OnPostRunResultUpdated"));
        }

        private void ApplyStatus(IPostRunResultService resultService)
        {
            UpdateTextsFromStatus(resultService.Result, resultService.Reason);
        }

        private void UpdateTextsFromStatus(PostRunResult result, string reason)
        {
            switch (result)
            {
                case PostRunResult.Victory:
                    SetTitle("Victory!");
                    break;
                case PostRunResult.Defeat:
                    SetTitle("Defeat!");
                    break;
                case PostRunResult.Exit:
                    SetTitle("Match Ended");
                    break;
                default:
                    SetTitle("Match Ended");
                    break;
            }

            SetReason(reason);
        }

        private void SetTitle(string text)
        {
            if (titleText == null)
            {
                return;
            }

            titleText.text = text;
        }

        private void SetReason(string reason)
        {
            if (reasonText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                reasonText.text = string.Empty;
                reasonText.gameObject.SetActive(false);
                return;
            }

            reasonText.text = reason;
            reasonText.gameObject.SetActive(true);
        }

        private void Show()
        {
            if (_isVisible)
            {
                return;
            }

            _isVisible = true;
            SetVisible(true);
        }

        private void HideImmediate()
        {
            if (!_isVisible)
            {
                SetVisible(false);
                return;
            }

            _isVisible = false;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (rootCanvasGroup == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>(
                    "[PostRunMenu] CanvasGroup nao configurado. Nao foi possivel alterar visibilidade.");
                return;
            }

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        private void ClosePostRun(string nextState, string reason)
        {
            var resultService = ResolveRequiredResultService("ClosePostRun");
            var currentScene = SceneManager.GetActiveScene().name;
            var result = resultService.Result;

            if (result == PostRunResult.None)
            {
                HardFailFastH1.Trigger(typeof(PostRunOverlayController),
                    $"[FATAL][H1][PostRun] Resultado do post-run nao consolidado ao fechar. nextState='{nextState}' reason='{reason}'.");
                return;
            }

            if (_postGameOwnership == null)
            {
                HardFailFastH1.Trigger(typeof(PostRunOverlayController),
                    $"[FATAL][H1][PostRun] IPostRunOwnershipService indisponivel ao fechar post-run. nextState='{nextState}' reason='{reason}'.");
                return;
            }

            _postGameOwnership.OnPostRunExited(new PostRunOwnershipExitContext(
                signature: currentScene,
                sceneName: currentScene,
                profile: string.Empty,
                frame: Time.frameCount,
                reason: reason,
                nextState: nextState,
                result: result));
        }

        private bool ShouldShowOverlayOnStart()
        {
            if (_postGameOwnership == null || !_postGameOwnership.IsActive)
            {
                return false;
            }

            var resultService = ResolveRequiredResultService("Start");
            return resultService.HasResult;
        }

        private IPostRunResultService ResolveRequiredResultService(string reason)
        {
            EnsureDependenciesInjected();

            if (_postGameResultService != null)
            {
                return _postGameResultService;
            }

            HardFailFastH1.Trigger(typeof(PostRunOverlayController),
                $"[FATAL][H1][PostRun] IPostRunResultService indisponivel. reason='{reason}'.");
            return null;
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.InjectDependencies(this);
                _dependenciesInjected = true;
            }
            catch
            {
                _dependenciesInjected = false;
            }
        }

        private void ValidateReferences()
        {
            if (rootCanvasGroup == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>("[PostRunMenu] rootCanvasGroup nao configurado no Inspector.");
            }

            if (titleText == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>("[PostRunMenu] titleText nao configurado no Inspector.");
            }

            if (reasonText == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>("[PostRunMenu] reasonText nao configurado no Inspector.");
            }

            if (restartButton == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>("[PostRunMenu] restartButton nao configurado no Inspector.");
            }

            if (exitToMenuButton == null)
            {
                DebugUtility.LogWarning<PostRunOverlayController>("[PostRunMenu] exitToMenuButton nao configurado no Inspector.");
            }
        }
    }
}

