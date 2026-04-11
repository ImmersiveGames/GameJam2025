using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NewRunDecisionEnteredEvent = _ImmersiveGames.NewScripts.Experience.PostRun.Contracts.RunDecisionEnteredEvent;
using NewRunDecisionCompletedEvent = _ImmersiveGames.NewScripts.Experience.PostRun.Contracts.RunDecisionCompletedEvent;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation.Bindings
{
    /// <summary>
    /// Contexto visual local de RunDecision.
    ///
    /// Consome a projeção canônica de resultado do PostRun e emite intents downstream.
    /// Gate, ownership e projeção de resultado ficam fora da camada visual.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed partial class PostRunOverlayController : MonoBehaviour, IRunDecisionStagePresenter
    {
        private const string RestartReason = "RunDecision/Restart";
        private const string ExitToMenuReason = "RunDecision/ExitToMenu";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitToMenuButton;

        [Inject] private IRunDecisionOwnershipService _runDecisionOwnershipService;
        [Inject] private IPostRunResultService _postRunResultService;
        [Inject] private IGameplaySessionFlowContinuityService _sessionFlowContinuityService;
        [Inject] private IRunDecisionStagePresenterHost _presenterHost;

        private bool _dependenciesInjected;
        private EventBinding<NewRunDecisionEnteredEvent> _runDecisionEnteredBinding;
        private EventBinding<NewRunDecisionCompletedEvent> _runDecisionCompletedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private bool _isVisible;
        private bool _actionRequested;

        public string PresenterSignature { get; private set; } = string.Empty;

        public bool IsReady => gameObject != null &&
                               gameObject.activeInHierarchy &&
                               !string.IsNullOrWhiteSpace(PresenterSignature);

        private void Awake()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            EnsureDependenciesInjected();

            _runDecisionEnteredBinding = new EventBinding<NewRunDecisionEnteredEvent>(OnRunDecisionEntered);
            _runDecisionCompletedBinding = new EventBinding<NewRunDecisionCompletedEvent>(OnRunDecisionCompleted);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            RegisterBindings();
            EnsureTypedRegistration();
            HideImmediate();
            ValidateReferences();
        }

        private void Start()
        {
            EnsureDependenciesInjected();
        }

        private void OnEnable() => RegisterBindings();

        private void OnDisable()
        {
            UnregisterBindings();
            if (_presenterHost != null)
            {
                _presenterHost.TryReleasePresenter(this, "OnDisable");
            }
        }

        private void OnDestroy()
        {
            UnregisterBindings();
        }

        public void BindToRunDecision(RunDecision decision)
        {
            PresenterSignature = Normalize(decision.Signature);
            DebugUtility.Log<IRunDecisionStagePresenter>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionPresenterAttached presenter='RunDecisionStagePresenter' scene='{gameObject.scene.name}' signature='{PresenterSignature}'.",
                DebugUtility.Colors.Info);

            ApplyStatus(ResolveRequiredResultService("BindToRunDecision"));
            Show();
        }

        public void DetachFromRunDecision(string reason)
        {
            HideImmediate();
            PresenterSignature = string.Empty;

            DebugUtility.Log<IRunDecisionStagePresenter>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionPresenterDetached presenter='RunDecisionStagePresenter' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botao de restart.
        /// </summary>
        public void OnClickRestart()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                    "[OBS][GameplaySessionFlow][RunDecision] Restart ignorado (acao ja solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision][Intent] Restart solicitado. Intent capturada e encaminhada ao executor downstream.",
                DebugUtility.Colors.Info);

            CloseRunDecision(RunDecisionCompletionKind.Restart, "DownstreamHandoff", RestartReason);
            HideImmediate();

            _ = ExecuteDownstreamActionAsync(
                actionName: "RestartGameplay",
                reason: RestartReason,
                action: ct => _sessionFlowContinuityService.RestartGameplayAsync(RestartReason, ct));
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botao de voltar ao menu.
        /// </summary>
        public void OnClickExitToMenu()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                    "[OBS][GameplaySessionFlow][RunDecision] ExitToMenu ignorado (acao ja solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision][Intent] ExitToMenu solicitado. Intent capturada e encaminhada ao executor downstream.",
                DebugUtility.Colors.Info);

            CloseRunDecision(RunDecisionCompletionKind.Menu, "FrontendMenu", ExitToMenuReason);
            HideImmediate();

            _ = ExecuteDownstreamActionAsync(
                actionName: "ExitToMenu",
                reason: ExitToMenuReason,
                action: ct => _sessionFlowContinuityService.ExitToMenuAsync(ExitToMenuReason, ct));
        }

        private async Task ExecuteDownstreamActionAsync(string actionName, string reason, Func<CancellationToken, Task> action)
        {
            if (_sessionFlowContinuityService == null)
            {
                HardFailFastH1.Trigger(typeof(IRunDecisionStagePresenter),
                    $"[FATAL][H1][RunDecision] IGameplaySessionFlowContinuityService indisponivel. action='{actionName}' reason='{reason}'.");
                return;
            }

            try
            {
                DebugUtility.Log<IRunDecisionStagePresenter>(
                    $"[OBS][GameplaySessionFlow][RunDecision][Delegate] {actionName} entregue ao executor downstream real IGameplaySessionFlowContinuityService. reason='{reason}'.");
                await action(CancellationToken.None);
                DebugUtility.Log<IRunDecisionStagePresenter>(
                    $"[OBS][GameplaySessionFlow][RunDecision][Execute] {actionName} executado pelo executor downstream. reason='{reason}'.");
            }
            catch (Exception ex)
            {
                _actionRequested = false;
                DebugUtility.LogWarning<IRunDecisionStagePresenter>(
                    $"[OBS][GameplaySessionFlow][RunDecision] {actionName} falhou. reason='{reason}', notes='{ex.GetType().Name}'.");
            }
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<NewRunDecisionEnteredEvent>.Register(_runDecisionEnteredBinding);
            EventBus<NewRunDecisionCompletedEvent>.Register(_runDecisionCompletedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision] Bindings de RunDecisionEntered/RunDecisionCompleted/GameRunStarted registrados.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<NewRunDecisionEnteredEvent>.Unregister(_runDecisionEnteredBinding);
            EventBus<NewRunDecisionCompletedEvent>.Unregister(_runDecisionCompletedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;

            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision] Bindings de RunDecisionEntered/RunDecisionCompleted/GameRunStarted removidos.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision] GameRunStartedEvent recebido. Ocultando contexto visual local.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
        }

        private void OnRunDecisionEntered(NewRunDecisionEnteredEvent evt)
        {
            if (_actionRequested)
            {
                return;
            }

            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision] RunDecisionEnteredEvent recebido. Exibindo contexto visual local.",
                DebugUtility.Colors.Info);

            BindToRunDecision(evt.Decision);
            LogOverlayOpened("RunDecisionEntered");
        }

        private void OnRunDecisionCompleted(NewRunDecisionCompletedEvent evt)
        {
            DebugUtility.LogVerbose<IRunDecisionStagePresenter>(
                "[OBS][GameplaySessionFlow][RunDecision] RunDecisionCompletedEvent recebido. Ocultando contexto visual.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
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
                DebugUtility.LogWarning<IRunDecisionStagePresenter>(
                    "[OBS][GameplaySessionFlow][RunDecision] CanvasGroup nao configurado. Nao foi possivel alterar visibilidade.");
                return;
            }

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        private void CloseRunDecision(RunDecisionCompletionKind completionKind, string handoffState, string reason)
        {
            var resultService = ResolveRequiredResultService("CloseRunDecision");
            var result = resultService.Result;

            if (result == PostRunResult.None)
            {
                HardFailFastH1.Trigger(typeof(IRunDecisionStagePresenter),
                    $"[FATAL][H1][GameplaySessionFlow] Resultado do post-run nao consolidado ao fechar. handoffState='{handoffState}' reason='{reason}'.");
                return;
            }

            if (_runDecisionOwnershipService == null)
            {
                HardFailFastH1.Trigger(typeof(IRunDecisionStagePresenter),
                    $"[FATAL][H1][GameplaySessionFlow] IRunDecisionOwnershipService indisponivel ao fechar RunDecision. handoffState='{handoffState}' reason='{reason}'.");
                return;
            }

            _runDecisionOwnershipService.ExitRunDecision(new RunDecisionCompletion(
                completionKind,
                reason,
                handoffState));
        }

        private void EnsureTypedRegistration()
        {
            if (_presenterHost == null)
            {
                HardFailFastH1.Trigger(typeof(IRunDecisionStagePresenter),
                    "[FATAL][H1][RunDecision] IRunDecisionStagePresenterHost indisponivel para registro tipado.");
                return;
            }

            _presenterHost.TryAdoptPresenter(this, nameof(IRunDecisionStagePresenter));
        }

        private void LogOverlayOpened(string reason)
        {
            DebugUtility.Log<IRunDecisionStagePresenter>(
                $"[OBS][GameplaySessionFlow][RunDecision] OverlayOpened reason='{reason}' scene='{SceneManager.GetActiveScene().name}'.",
                DebugUtility.Colors.Info);
        }

        private IPostRunResultService ResolveRequiredResultService(string reason)
        {
            EnsureDependenciesInjected();

            if (_postRunResultService != null)
            {
                return _postRunResultService;
            }

            HardFailFastH1.Trigger(typeof(IRunDecisionStagePresenter),
                $"[FATAL][H1][RunDecision] IPostRunResultService indisponivel. reason='{reason}'.");
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
                DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] rootCanvasGroup nao configurado no Inspector.");
            }

            if (titleText == null)
            {
                DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] titleText nao configurado no Inspector.");
            }

            if (reasonText == null)
            {
                DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] reasonText nao configurado no Inspector.");
            }

            if (restartButton == null)
            {
                DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] restartButton nao configurado no Inspector.");
            }

            if (exitToMenuButton == null)
            {
                DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] exitToMenuButton nao configurado no Inspector.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
