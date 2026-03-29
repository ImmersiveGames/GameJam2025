using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.NewScripts.Modules.PostGame.Bindings
{
    /// <summary>
    /// Controller do contexto visual downstream de PostRunMenu.
    /// Nota: a pausa da simulação em Victory/Defeat é solicitada pelo GameRunResultSnapshotService.
    /// Este overlay apenas exibe UI e publica intents downstream (Restart / ExitToMenu).
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed partial class PostGameOverlayController : MonoBehaviour
    {
        private const string RestartReason = "PostGame/Restart";
        private const string ExitToMenuReason = "PostGame/ExitToMenu";
        private const string PostGameGateToken = "state.postgame";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitToMenuButton;

                [Inject] private ISimulationGateService _gateService;
        [Inject] private IPostGameOwnershipService _postGameOwnership;
        [Inject] private IPostLevelActionsService _postLevelActionsService;

        private bool _dependenciesInjected;
        private EventBinding<PostGameEnteredEvent> _postGameEnteredBinding;
        private EventBinding<PostGameExitedEvent> _postGameExitedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private IDisposable _postGameGateHandle;
        private bool _loggedMissingGate;
        private bool _isVisible;
        private bool _actionRequested;

        private void Awake()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            _postGameEnteredBinding = new EventBinding<PostGameEnteredEvent>(OnPostGameEntered);
            _postGameExitedBinding = new EventBinding<PostGameExitedEvent>(OnPostGameExited);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            RegisterBindings();
            HideImmediate();

            ValidateReferences();
        }

        private void Start()
        {
            EnsureDependenciesInjected();

            if (ShouldShowOverlayOnStart(out var statusService))
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostRunMenu] Resultado pré-existente detectado na inicialização. Exibindo menu após contexto downstream ativo.",
                    DebugUtility.Colors.Info);
                ApplyStatus(statusService);
                Show();
            }
        }

        private void OnEnable() => RegisterBindings();

        private void OnDisable()
        {
            UnregisterBindings();
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("OnDisable");
            }
        }

        private void OnDestroy()
        {
            UnregisterBindings();
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("OnDestroy");
            }
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de restart.
        /// </summary>
        public void OnClickRestart()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostRunMenu] Restart ignorado (ação já solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            // Ao solicitar restart, o overlay deixa de fazer sentido imediatamente.
            HideImmediate();

            _ = ExecutePostLevelActionAsync(
                actionName: "RestartLevel",
                reason: RestartReason,
                action: ct => _postLevelActionsService.RestartLevelAsync(RestartReason, ct));
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de voltar ao menu.
        /// </summary>
        public void OnClickExitToMenu()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostRunMenu] ExitToMenu ignorado (ação já solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            // Ao sair para o menu, o overlay não é mais relevante — ocultamos imediatamente.
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
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[PostRunMenu] {actionName} cancelado: IPostLevelActionsService indisponível. reason='{reason}'.");
                _actionRequested = false;
                return;
            }

            try
            {
                await action(CancellationToken.None);
                DebugUtility.Log<PostGameOverlayController>(
                    $"[PostRunMenu] {actionName} solicitado via overlay. reason='{reason}'.");
            }
            catch (Exception ex)
            {
                _actionRequested = false;
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[PostRunMenu] {actionName} falhou. reason='{reason}', notes='{ex.GetType().Name}'.");
            }
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<PostGameEnteredEvent>.Register(_postGameEnteredBinding);
            EventBus<PostGameExitedEvent>.Register(_postGameExitedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] Bindings de PostGameEntered/PostGameExited/GameRunStarted registrados.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<PostGameEnteredEvent>.Unregister(_postGameEnteredBinding);
            EventBus<PostGameExitedEvent>.Unregister(_postGameExitedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] Bindings de PostGameEntered/PostGameExited/GameRunStarted removidos.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] GameRunStartedEvent recebido. Ocultando menu.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
            if (ShouldOverlayManageOwnership())
            {
                ApplyGameplayInputMode("PostGame/RunStarted");
            }
        }

        private void OnPostGameEntered(PostGameEnteredEvent evt)
        {
            if (_actionRequested)
            {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] PostRunMenuEnteredEvent ignorado (ação já solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_isVisible)
            {
                return;
            }

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] PostRunMenuEnteredEvent recebido. Exibindo menu.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunResultSnapshotService>(out var statusService)
                || statusService == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostRunMenu] IGameRunResultSnapshotService indisponível. Usando fallback de texto.");
                ApplyFallbackText();
                Show();
                return;
            }

            ApplyStatus(statusService);
            Show();
        }

        private void OnPostGameExited(PostGameExitedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostRunMenu] PostRunMenuExitedEvent recebido. Ocultando menu.",
                DebugUtility.Colors.Info);

            _actionRequested = false;
            HideImmediate();
        }

        private void ApplyStatus(IGameRunResultSnapshotService resultSnapshotService)
        {
            UpdateTextsFromStatus(resultSnapshotService.Outcome, resultSnapshotService.Reason);
        }

        private void UpdateTextsFromStatus(GameRunOutcome outcome, string reason)
        {
            switch (outcome)
            {
                case GameRunOutcome.Victory:
                    SetTitle("Victory!");
                    break;
                case GameRunOutcome.Defeat:
                    SetTitle("Defeat!");
                    break;
                default:
                    SetTitle("Match Ended");
                    break;
            }

            SetReason(reason);
        }

        private void ApplyFallbackText()
        {
            SetTitle("Match Ended");
            SetReason("Resultado indisponível.");
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
            _actionRequested = false;
            SetVisible(true);
            if (ShouldOverlayManageOwnership())
            {
                ApplyPostGameInputMode("PostGame/Show");
                AcquirePostGameGate();
            }
        }

        private void HideImmediate()
        {
            SetVisible(false);

            if (!_isVisible)
            {
                return;
            }

            _isVisible = false;
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("HideImmediate");
            }
        }

        private void SetVisible(bool visible)
        {
            if (rootCanvasGroup == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostRunMenu] CanvasGroup não configurado. Não foi possível alterar visibilidade.");
                return;
            }

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        private void ApplyPostGameInputMode(string reason)
        {
            EnsureDependenciesInjected();
            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.FrontendMenu, reason, "PostGameOverlay"));
        }

        private void ApplyGameplayInputMode(string reason)
        {
            EnsureDependenciesInjected();
            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.Gameplay, reason, "PostGameOverlay"));
        }

        private bool ShouldShowOverlayOnStart(out IGameRunResultSnapshotService statusService)
        {
            statusService = null;

            if (_postGameOwnership == null || !_postGameOwnership.IsActive)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out statusService) || statusService == null)
            {
                return false;
            }

            return statusService.HasResult;
        }

        private bool ShouldOverlayManageOwnership()
        {
            return _postGameOwnership == null || !_postGameOwnership.IsOwnerEnabled;
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

        private void AcquirePostGameGate()
        {
            EnsureDependenciesInjected();

            if (_postGameGateHandle != null)
            {
                return;
            }

            if (_gateService == null)
            {
                if (!_loggedMissingGate)
                {
                DebugUtility.LogWarning<PostGameOverlayController>(
                        "[PostRunMenu] ISimulationGateService indisponível. Gate não será adquirido.");
                    _loggedMissingGate = true;
                }

                return;
            }

            _postGameGateHandle = _gateService.Acquire(PostGameGateToken);
            DebugUtility.Log<PostGameOverlayController>(
                $"[PostRunMenu] Gate adquirido token='{PostGameGateToken}'.");
        }

        private void ReleasePostGameGate(string reason)
        {
            if (_postGameGateHandle == null)
            {
                return;
            }

            try
            {
                _postGameGateHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[PostRunMenu] Falha ao liberar gate ({reason}): {ex}");
            }
            finally
            {
                _postGameGateHandle = null;
            }

            DebugUtility.Log<PostGameOverlayController>(
                $"[PostRunMenu] Gate liberado token='{PostGameGateToken}'.");
        }

        private void ValidateReferences()
        {
            if (rootCanvasGroup == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>("[PostRunMenu] rootCanvasGroup não configurado no Inspector.");
            }
            if (titleText == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>("[PostRunMenu] titleText não configurado no Inspector.");
            }
            if (reasonText == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>("[PostRunMenu] reasonText não configurado no Inspector.");
            }
            if (restartButton == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>("[PostRunMenu] restartButton não configurado no Inspector.");
            }
            if (exitToMenuButton == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>("[PostRunMenu] exitToMenuButton não configurado no Inspector.");
            }
        }
    }
}




