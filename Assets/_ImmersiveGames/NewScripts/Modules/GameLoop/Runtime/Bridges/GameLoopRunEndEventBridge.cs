using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges
{
    /// <summary>
    /// Bridge do fim de run: GameRunEndedEvent -> IGameLoopService.RequestEnd().
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopRunEndEventBridge : MonoBehaviour
    {
        private EventBinding<GameRunEndedEvent> _binding;
        private bool _registered;

        private void Awake()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            RegisterBinding();
        }

        private void OnEnable()
        {
            RegisterBinding();
        }

        private void OnDisable()
        {
            UnregisterBinding();
        }

        private void OnDestroy()
        {
            UnregisterBinding();
        }

        private void RegisterBinding()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Register(_binding);
            _registered = true;
        }

        private void UnregisterBinding()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Unregister(_binding);
            _registered = false;
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (!IsGameplayScene())
            {
                DebugUtility.LogWarning<GameLoopRunEndEventBridge>(
                    $"[OBS][PostGame] PostGameSkipped reason='scene_not_gameplay' scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                DebugUtility.LogWarning<GameLoopRunEndEventBridge>(
                    "[GameLoop] GameRunEndedEvent recebido mas IGameLoopService não foi encontrado no escopo global.");
                return;
            }

            string reason = evt?.Reason ?? "<null>";
            DebugUtility.Log<GameLoopRunEndEventBridge>(
                $"[GameLoop] GameRunEndedEvent recebido. Outcome={evt?.Outcome}, Reason='{reason}'. Sinalizando EndRequested.");

            gameLoopService.RequestEnd();
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            string sceneName = SceneManager.GetActiveScene().name;
            return string.Equals(sceneName, "GameplayScene", System.StringComparison.Ordinal);
        }
    }
}


