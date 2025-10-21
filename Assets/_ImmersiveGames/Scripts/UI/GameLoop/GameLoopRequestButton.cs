using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.UI.GameLoop
{
    public enum GameLoopRequestType
    {
        Start,
        Pause,
        Resume,
        Reset,
        GameOver,
        Victory,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("Immersive Games/Game Loop/Game Loop Request Button")]
    public sealed class GameLoopRequestButton : MonoBehaviour
    {
        [SerializeField] private GameLoopRequestType requestType = GameLoopRequestType.Start;

        // Método chamado pelo UnityEvent do botão para acionar o evento correspondente.
        public void RaiseRequest()
        {
            switch (requestType)
            {
                case GameLoopRequestType.Start:
                    EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
                    break;
                case GameLoopRequestType.Pause:
                    EventBus<GamePauseRequestedEvent>.Raise(new GamePauseRequestedEvent());
                    break;
                case GameLoopRequestType.Resume:
                    EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
                    break;
                case GameLoopRequestType.Reset:
                    EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
                    break;
                case GameLoopRequestType.GameOver:
                    EventBus<GameOverEvent>.Raise(new GameOverEvent());
                    break;
                case GameLoopRequestType.Victory:
                    EventBus<GameVictoryEvent>.Raise(new GameVictoryEvent());
                    break;
                default:
                    DebugUtility.LogWarning<GameLoopRequestButton>($"Unhandled request type: {requestType}");
                    break;
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Garante configuração padrão intuitiva durante a criação do componente.
            requestType = GameLoopRequestType.Start;
        }
#endif
    }
}
