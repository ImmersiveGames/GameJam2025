using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.InputSystems
{
    /// <summary>
    /// Bridge global para aplicar modo de input com base nos eventos do SceneFlow.
    /// Também sincroniza o GameLoop com a intenção do profile:
    /// - Gameplay: aplica InputMode e dispara a IntroStage (o início real ocorre após conclusão explícita da IntroStage).
    /// - Startup/Frontend: garante que o GameLoop não fique ativo em menu/frontend.
    /// </summary>
    public sealed class InputModeSceneFlowBridge : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        public InputModeSceneFlowBridge()
        {
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                "[InputMode] InputModeSceneFlowBridge registrado nos eventos de SceneTransitionCompletedEvent.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                "[InputModeSceneFlowBridge] [GameLoop] Bridge registrado para SceneTransitionCompletedEvent (Frontend/GamePlay -> GameLoop sync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
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

            var profile = evt.Context.TransitionProfileName;

            // ===== Gameplay =====
            if (string.Equals(profile, SceneFlowProfileNames.Gameplay, StringComparison.OrdinalIgnoreCase))
            {
                inputModeService.SetGameplay("SceneFlow/Completed:Gameplay");

                var gameLoopService = ResolveGameLoopService();
                if (gameLoopService == null)
                {
                    DebugUtility.LogWarning<InputModeSceneFlowBridge>(
                        "[InputModeSceneFlowBridge] [GameLoop] IGameLoopService indisponivel; sincronizacao ignorada.");
                    return;
                }

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

                if (!IsGameplayScene())
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [IntroStage] Cena ativa não é gameplay. IntroStage ignorada. scene='{SceneManager.GetActiveScene().name}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    
                    // Evita disparo duplicado:
                    // - PhaseStartPhaseCommitBridge agenda IntroStage após TransitionCompleted quando uma PhaseCommitted ocorre durante SceneTransition.
                    // - Este bridge dispara IntroStage em SceneFlow/Completed para o caso "entrada no gameplay" sem pipeline pendente.
                    if (DependencyManager.Provider.TryGetGlobal<PhaseStartPhaseCommitBridge>(out var phaseBridge)
                        && phaseBridge != null
                        && phaseBridge.HasPendingFor(evt.Context.ContextSignature))
                    {
                        DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                            $"[InputModeSceneFlowBridge] [IntroStage] Suprimida (PhaseStart pipeline pendente). signature='{evt.Context.ContextSignature}'.",
                            DebugUtility.Colors.Info);
                        return;
                    }

var coordinator = ResolveIntroStageCoordinator();
                    if (coordinator == null)
                    {
                        DebugUtility.LogWarning<InputModeSceneFlowBridge>(
                            "[InputModeSceneFlowBridge] [IntroStage] IIntroStageCoordinator indisponível; IntroStage não será executada.");
                    }
                    else
                    {
                        var signature = SceneTransitionSignatureUtil.Compute(evt.Context);
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
            if (string.Equals(profile, SceneFlowProfileNames.Startup, StringComparison.OrdinalIgnoreCase)
                || string.Equals(profile, SceneFlowProfileNames.Frontend, StringComparison.OrdinalIgnoreCase))
            {
                inputModeService.SetFrontendMenu("SceneFlow/Completed:Frontend");

                // Correção-alvo:
                // Menu/Frontend não deve “rodar run”. Se algo iniciou o GameLoop antes,
                // encerramos a run aqui para evitar ficar em Playing no menu.
                var gameLoopService = ResolveGameLoopService();
                if (gameLoopService == null)
                {
                    return;
                }

                var state = gameLoopService.CurrentStateIdName;

                if (string.Equals(state, nameof(GameLoopStateId.Playing), StringComparison.Ordinal)
                    || string.Equals(state, nameof(GameLoopStateId.Paused), StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                        $"[InputModeSceneFlowBridge] [GameLoop] Frontend completed com estado ativo ('{state}'). " +
                        "Solicitando RequestEnd() para garantir menu inativo.",
                        DebugUtility.Colors.Info);

                    gameLoopService.RequestEnd();
                }

                return;
            }

            DebugUtility.LogVerbose<InputModeSceneFlowBridge>(
                $"[InputMode] Profile nao reconhecido ('{profile}'); input mode nao alterado.",
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
