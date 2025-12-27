using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.InputSystems
{
    /// <summary>
    /// Bridge global para aplicar modo de input com base nos eventos do SceneFlow.
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
            if (string.Equals(profile, SceneFlowProfileNames.Gameplay, StringComparison.OrdinalIgnoreCase))
            {
                inputModeService.SetGameplay("SceneFlow/Completed:Gameplay");
                return;
            }

            if (string.Equals(profile, SceneFlowProfileNames.Startup, StringComparison.OrdinalIgnoreCase)
                || string.Equals(profile, SceneFlowProfileNames.Frontend, StringComparison.OrdinalIgnoreCase))
            {
                inputModeService.SetFrontendMenu("SceneFlow/Completed:Frontend");
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
    }
}
