using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
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
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                DebugUtility.LogWarning<GameLoopRunEndEventBridge>(
                    "[GameLoop] GameRunEndedEvent recebido mas IGameLoopService não foi encontrado no escopo global.");
                return;
            }

            var reason = evt?.Reason ?? "<null>";
            DebugUtility.Log<GameLoopRunEndEventBridge>(
                $"[GameLoop] GameRunEndedEvent recebido. Outcome={evt?.Outcome}, Reason='{reason}'. Sinalizando EndRequested.");

            gameLoopService.RequestEnd();
        }
    }
}
