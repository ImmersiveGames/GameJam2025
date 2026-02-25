using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.InputModes.Interop
{
    /// <summary>
    /// Bridge global para aplicar modo de input com base nos eventos do SceneFlow.
    /// Também sincroniza o GameLoop com a intenção do profile:
    /// - Gameplay: aplica InputMode.
    /// - Startup/Frontend: garante que o GameLoop não fique ativo em menu/frontend.
    /// </summary>
    public sealed class SceneFlowInputModeBridge : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;

        // Dedupe por instância para evitar supressão entre instâncias após restarts.
        private string _lastProcessedSignature;

        public SceneFlowInputModeBridge()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                "[InputMode] SceneFlowInputModeBridge registrado nos eventos de SceneTransitionStartedEvent e SceneTransitionCompletedEvent.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                "[SceneFlowInputModeBridge] [GameLoop] Bridge registrado para SceneTransitionCompletedEvent (Frontend/GamePlay -> GameLoop sync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.Context);
            _lastProcessedSignature = string.Empty;

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                $"[SceneFlowInputModeBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='{signature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            var inputModeService = ResolveInputModeService();
            if (inputModeService == null)
            {
                DebugUtility.LogWarning<SceneFlowInputModeBridge>(
                    "[InputMode] IInputModeService indisponivel; transicao ignorada.");
                ReportInputModesDegraded("missing_service",
                    "SceneFlowInputModeBridge could not resolve IInputModeService.");
                return;
            }

            string profile = evt.Context.TransitionProfileName;
            string signature = SceneTransitionSignature.Compute(evt.Context);
            string dedupeKey = $"{profile}|{signature}";
            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;

            // ===== Gameplay =====
            if (evt.Context.TransitionProfileId == SceneFlowProfileId.Gameplay)
            {
                inputModeService.SetGameplay("SceneFlow/Completed:Gameplay");

                LogObsInputModeApplied(
                    mode: "Gameplay",
                    map: "Player",
                    reason: "SceneFlow/Completed:Gameplay",
                    signature: signature,
                    scene: activeScene,
                    profile: profile);

                var gameLoopService = ResolveGameLoopService();
                if (gameLoopService == null)
                {
                    DebugUtility.LogWarning<SceneFlowInputModeBridge>(
                        "[SceneFlowInputModeBridge] [GameLoop] IGameLoopService indisponivel; sincronizacao ignorada.");
                    return;
                }

                bool isBootState = string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Boot), StringComparison.Ordinal);
                if (!isBootState
                    && !string.IsNullOrWhiteSpace(_lastProcessedSignature)
                    && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        $"[SceneFlowInputModeBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (isBootState)
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        "[SceneFlowInputModeBridge] [GameLoop] Estado=Boot -> bypass dedupe (Restart/Boot cycle).",
                        DebugUtility.Colors.Info);
                }

                _lastProcessedSignature = dedupeKey;

                DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                    "[SceneFlowInputModeBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop.",
                    DebugUtility.Colors.Info);

                // Se veio de um PauseOverlay por qualquer motivo, prefira Resume.
                if (string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Paused), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        "[SceneFlowInputModeBridge] [GameLoop] Estado=Paused -> RequestResume().",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestResume();
                    return;
                }

                // Se já está Playing, não faz nada (evita ruído).
                if (string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        "[SceneFlowInputModeBridge] [GameLoop] RequestStart ignored (already active). " +
                        "source=SceneFlow/Completed:Gameplay state=Playing.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (isBootState)
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        "[SceneFlowInputModeBridge] [GameLoop] Estado=Boot -> RequestReady() para habilitar fluxo de gameplay.",
                        DebugUtility.Colors.Info);
                    gameLoopService.RequestReady();
                }

                return;
            }

            // ===== Frontend/Startup =====
            if (evt.Context.TransitionProfileId.IsStartupOrFrontend)
            {
                if (!string.IsNullOrWhiteSpace(_lastProcessedSignature)
                    && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        $"[SceneFlowInputModeBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _lastProcessedSignature = dedupeKey;
                inputModeService.SetFrontendMenu("SceneFlow/Completed:Frontend");

                LogObsInputModeApplied(
                    mode: "FrontendMenu",
                    map: "UI",
                    reason: "SceneFlow/Completed:Frontend",
                    signature: signature,
                    scene: activeScene,
                    profile: profile);

                // Correção-alvo:
                // Menu/Frontend não deve “rodar run”. Se algo iniciou o GameLoop antes,
                // encerramos a run aqui para evitar ficar em Playing no menu.
                var gameLoopService = ResolveGameLoopService();
                if (gameLoopService == null)
                {
                    return;
                }

                string state = gameLoopService.CurrentStateIdName;

                if (string.Equals(state, nameof(GameLoopStateId.Playing), StringComparison.Ordinal)
                    || string.Equals(state, nameof(GameLoopStateId.Paused), StringComparison.Ordinal)
                    || string.Equals(state, nameof(GameLoopStateId.IntroStage), StringComparison.Ordinal)
                    || string.Equals(state, nameof(GameLoopStateId.PostPlay), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                        $"[SceneFlowInputModeBridge] [GameLoop] Frontend completed com estado ativo ('{state}'). " +
                        "Solicitando RequestReady() para garantir menu inativo.",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestReady();
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(_lastProcessedSignature)
                && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                    $"[SceneFlowInputModeBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = dedupeKey;

            DebugUtility.LogVerbose<SceneFlowInputModeBridge>(
                $"[InputMode] Profile nao reconhecido ('{profile}'); input mode nao alterado.",
                DebugUtility.Colors.Info);
        }

        private static void LogObsInputModeApplied(
            string mode,
            string map,
            string reason,
            string signature,
            string scene,
            string profile)
        {
            // Observabilidade canônica (Contrato): Request mode/map/reason/signature/scene/profile.
            DebugUtility.LogVerbose(typeof(SceneFlowInputModeBridge),
                $"[OBS][InputMode] Requested mode='{mode}' map='{map}' signature='{signature ?? string.Empty}' scene='{scene ?? string.Empty}' profile='{profile ?? string.Empty}' reason='{reason ?? string.Empty}' (delegated).",
                DebugUtility.Colors.Info);
        }

        private static IInputModeService ResolveInputModeService()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service) ? service : null;
        }

        private static void ReportInputModesDegraded(string reason, string detail)
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var reporter) || reporter == null)
            {
                return;
            }

            reporter.Report(DegradedKeys.Feature.InputModes, reason, detail);
        }

        private static IGameLoopService ResolveGameLoopService()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) ? service : null;
        }
    }
}
