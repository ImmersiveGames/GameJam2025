using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Runtime.Scene;
using _ImmersiveGames.NewScripts.Runtime.Scene;
using _ImmersiveGames.NewScripts.Runtime.SceneFlow;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Runtime.InputSystems
{
    /// <summary>
    /// Bridge global para aplicar modo de input com base nos eventos do SceneFlow.
    /// Também sincroniza o GameLoop com a intenção do profile:
    /// - Gameplay: aplica InputMode e dispara a IntroStageController (o início real ocorre após conclusão explícita da IntroStageController).
    /// - Startup/Frontend: garante que o GameLoop não fique ativo em menu/frontend.
    /// </summary>
    public sealed class InputModeSceneFlowBridge : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        // Dedupe por instância para evitar supressão entre instâncias após restarts.
        private string _lastProcessedSignature;

        public InputModeSceneFlowBridge()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                "[InputMode] InputModeSceneFlowBridge registrado nos eventos de SceneTransitionStartedEvent e SceneTransitionCompletedEvent.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                "[InputModeSceneFlowBridge] [GameLoop] Bridge registrado para SceneTransitionCompletedEvent (Frontend/GamePlay -> GameLoop sync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);
            _lastProcessedSignature = string.Empty;

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                $"[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Started -> reset dedupe. signature='{signature}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            var inputModeService = ResolveInputModeService();
            if (inputModeService == null)
            {
                DebugUtility.LogWarning<InputModeSceneFlowBridge>(
                    "[InputMode] IInputModeService indisponivel; transicao ignorada.");
                return;
            }

            string profile = evt.Context.TransitionProfileName;
            string signature = SceneTransitionSignatureUtil.Compute(evt.Context);
            string dedupeKey = $"{profile}|{signature}";
            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;

            // ===== Gameplay =====
            if (evt.Context.TransitionProfileId == SceneFlowProfileId.Gameplay)
            {
                inputModeService.SetGameplay("SceneFlow/Completed:Gameplay");

                LogObsInputModeApplied(
                    mode: "Gameplay",
                    map: "Gameplay",
                    reason: "SceneFlow/Completed:Gameplay",
                    signature: signature,
                    scene: activeScene,
                    profile: profile);

                var gameLoopService = ResolveGameLoopService();
                if (gameLoopService == null)
                {
                    DebugUtility.LogWarning<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] IGameLoopService indisponivel; sincronizacao ignorada.");
                    return;
                }

                bool isBootState = string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Boot), StringComparison.Ordinal);
                if (!isBootState
                    && !string.IsNullOrWhiteSpace(_lastProcessedSignature)
                    && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (isBootState)
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] Estado=Boot -> bypass dedupe (Restart/Boot cycle).",
                        DebugUtility.Colors.Info);
                }

                _lastProcessedSignature = dedupeKey;

                DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                    "[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> sincronizando GameLoop.",
                    DebugUtility.Colors.Info);

                // Se veio de um PauseOverlay por qualquer motivo, prefira Resume.
                if (string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Paused), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] Estado=Paused -> RequestResume().",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestResume();
                    return;
                }

                // Se já está Playing, não faz nada (evita ruído).
                if (string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] RequestStart ignored (already active). " +
                        "source=SceneFlow/Completed:Gameplay state=Playing.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (isBootState)
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] Estado=Boot -> RequestReady() para habilitar IntroStageController (Restart/Boot cycle).",
                        DebugUtility.Colors.Info);
                    gameLoopService.RequestReady();
                }

                if (!IsGameplayScene())
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [IntroStageController] Cena ativa não é gameplay. IntroStageController ignorada. scene='{SceneManager.GetActiveScene().name}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    var coordinator = ResolveIntroStageCoordinator();
                    if (coordinator == null)
                    {
                        DebugUtility.LogWarning<InputModeSceneFlowBridge>(
                            "[InputModeSceneFlowBridge] [IntroStageController] IIntroStageCoordinator indisponível; IntroStageController não será executada.");
                    }
                    else
                    {
                        gameLoopService.RequestIntroStageStart();
                        var introStageContext = new IntroStageContext(
                            contextSignature: signature,
                            profileId: evt.Context.TransitionProfileId,
                            targetScene: evt.Context.TargetActiveScene,
                            reason: "SceneFlow/Completed");

                        _ = coordinator.RunIntroStageAsync(introStageContext);
                    }
                }
                return;
            }

            // ===== Frontend/Startup =====
            if (evt.Context.TransitionProfileId.IsStartupOrFrontend)
            {
                if (!string.IsNullOrWhiteSpace(_lastProcessedSignature)
                    && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
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
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [GameLoop] Frontend completed com estado ativo ('{state}'). " +
                        "Solicitando RequestReady() para garantir menu inativo.",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestReady();
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(_lastProcessedSignature)
                && string.Equals(_lastProcessedSignature, dedupeKey, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                    $"[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed ignorado (assinatura já processada). signature='{signature}' profile='{profile}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = dedupeKey;

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
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
            // Observabilidade canônica (Contrato): Applied mode/map/reason/signature/scene/profile.
            DebugUtility.LogVerbose(typeof(InputModeSceneFlowBridge),
                $"[OBS][InputMode] Applied mode='{mode}' map='{map}' signature='{signature ?? string.Empty}' scene='{scene ?? string.Empty}' profile='{profile ?? string.Empty}' reason='{reason ?? string.Empty}'.",
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

        private static IGameLoopService ResolveGameLoopService()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) ? service : null;
        }

        private static IIntroStageCoordinator ResolveIntroStageCoordinator()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var service) ? service : null;
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return string.Equals(SceneManager.GetActiveScene().name, "GameplayScene", StringComparison.Ordinal);
        }
    }
}

