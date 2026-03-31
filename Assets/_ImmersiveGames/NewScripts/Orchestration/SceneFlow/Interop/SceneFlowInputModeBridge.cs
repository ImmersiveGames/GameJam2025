using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Interop
{
    /// <summary>
    /// Bridge global para sincronizar InputMode com base nos eventos do SceneFlow.
    /// Nao sincroniza estado de GameLoop diretamente.
    ///
    /// Semantica:
    /// - Gameplay: solicita InputMode de gameplay.
    /// - Startup/Frontend: solicita InputMode de menu.
    /// </summary>
    /// <summary>
    /// OWNER: sincronizacao de intencao de InputMode orientada por eventos de transicao.
    /// NAO E OWNER: execucao da transicao de cena e seus gates.
    /// PUBLISH/CONSUME: consome SceneTransitionStartedEvent e SceneTransitionCompletedEvent; publica apenas request events.
    /// Fases tocadas: TransitionStarted e TransitionCompleted.
    /// </summary>
    public sealed class SceneFlowInputModeBridge : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private bool _disposed;

        // Dedupe por instancia para evitar supressao entre instancias apos restarts.
        private string _lastProcessedSignature = string.Empty;
        private string _lastStartedSignature = string.Empty;
        private int _lastStartedFrame = -1;

        public SceneFlowInputModeBridge()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                "[InputMode] SceneFlowInputModeBridge registrado nos eventos de SceneTransitionStartedEvent e SceneTransitionCompletedEvent.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            int frame = UnityEngine.Time.frameCount;

            if (SceneFlowSameFrameDedupe.ShouldDedupe(
                    ref _lastStartedFrame,
                    ref _lastStartedSignature,
                    frame,
                    signature))
            {
                DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                    $"[OBS][GRS] InputModeBridge dedupe event='SceneTransitionStarted' signature='{signature}' frame={frame} reason='duplicate_same_frame'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = string.Empty;

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                $"[InputMode] SceneFlow/Started -> reset dedupe de input. signature='{signature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            string dedupeKey = signature;
            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_lastProcessedSignature)
                && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                    $"[InputMode] SceneFlow/Completed ignorado (assinatura ja processada). signature='{signature}' routeKind='{evt.context.RouteKind}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = dedupeKey;

            if (TryGetInputModeRequest(evt.context.RouteKind, out var requestKind, out var mode, out var map, out var reason))
            {
                EventBus<InputModeRequestEvent>.Raise(
                    new InputModeRequestEvent(requestKind, reason, "SceneFlow", signature));

                LogObsInputModeApplied(
                    mode: mode,
                    map: map,
                    reason: reason,
                    signature: signature,
                    scene: activeScene,
                    routeKind: evt.context.RouteKind);
                return;
            }

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                $"[InputMode] RouteKind nao reconhecido ('{evt.context.RouteKind}'); input mode nao alterado. targetScene='{evt.context.TargetActiveScene}'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryGetInputModeRequest(
            SceneRouteKind routeKind,
            out InputModeRequestKind requestKind,
            out string mode,
            out string map,
            out string reason)
        {
            switch (routeKind)
            {
                case SceneRouteKind.Gameplay:
                    requestKind = InputModeRequestKind.Gameplay;
                    mode = "Gameplay";
                    map = InputModesDefaults.PlayerActionMapName;
                    reason = "SceneFlow/Completed:Gameplay";
                    return true;
                case SceneRouteKind.Frontend:
                    requestKind = InputModeRequestKind.FrontendMenu;
                    mode = "FrontendMenu";
                    map = InputModesDefaults.MenuActionMapName;
                    reason = "SceneFlow/Completed:Frontend";
                    return true;
                default:
                    requestKind = default;
                    mode = string.Empty;
                    map = string.Empty;
                    reason = string.Empty;
                    return false;
            }
        }

        private static void LogObsInputModeApplied(
            string mode,
            string map,
            string reason,
            string signature,
            string scene,
            SceneRouteKind routeKind)
        {
            DebugUtility.LogVerbose(typeof(SceneFlowInputModeBridge),
                $"[OBS][InputMode] Requested mode='{mode}' map='{map}' signature='{signature ?? string.Empty}' scene='{scene ?? string.Empty}' routeKind='{routeKind}' reason='{reason ?? string.Empty}' (delegated).",
                DebugUtility.Colors.Info);
        }
    }
}








