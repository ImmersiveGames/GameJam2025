using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SceneManagement.Tests
{
    /// <summary>
    /// Componente de debug para observar o ciclo completo de transições de cena.
    /// Loga os eventos:
    /// - SceneTransitionStartedEvent
    /// - SceneTransitionScenesReadyEvent
    /// - SceneTransitionCompletedEvent
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionObserverTester : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private bool logStarted = true;
        [SerializeField] private bool logScenesReady = true;
        [SerializeField] private bool logCompleted = true;

        private EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private EventBinding<SceneTransitionCompletedEvent> _completedBinding;

        private void OnEnable()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.Log<SceneTransitionObserverTester>(
                "SceneTransitionObserverTester registrado.",
                DebugUtility.Colors.Info);
        }

        private void OnDisable()
        {
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);

            DebugUtility.LogVerbose<SceneTransitionObserverTester>(
                "SceneTransitionObserverTester removido.");
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!logStarted) return;

            DebugUtility.LogVerbose<SceneTransitionObserverTester>(
                $"[Started] Contexto = {evt.Context}");
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (!logScenesReady) return;

            DebugUtility.LogVerbose<SceneTransitionObserverTester>(
                $"[ScenesReady] Contexto = {evt.Context}");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!logCompleted) return;

            DebugUtility.LogVerbose<SceneTransitionObserverTester>(
                $"[Completed] Contexto = {evt.Context}");
        }
    }
}
