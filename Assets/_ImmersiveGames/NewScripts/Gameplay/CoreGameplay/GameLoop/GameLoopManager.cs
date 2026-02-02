#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop
{
    /// <summary>
    /// Substitui o antigo `GameLoop`. Nome de arquivo diferenciado para evitar conflito com
    /// o nome da pasta/namespace.
    /// Mantém a mesma API dos eventos e métodos de controle de estado.
    /// </summary>
    public sealed class GameLoopManager
    {
        public enum State
        {
            None,
            Intro,
            Playing,
            PostGame
        }

        private State _state = State.None;

        public event Action? GameplaySimulationBlocked;
        public event Action? GameplaySimulationUnblocked;
        public event Action<State>? EnteredState;

        public void StartIntro()
        {
            _state = State.Intro;
            GameplaySimulationBlocked?.Invoke();
            EnteredState?.Invoke(_state);
            DebugUtility.LogVerbose<GameLoopManager>("[OBS] GameLoopManager ENTER Intro");
        }

        public void ConfirmIntroComplete()
        {
            if (_state != State.Intro)
            {
                return;
            }

            GameplaySimulationUnblocked?.Invoke();
            _state = State.Playing;
            EnteredState?.Invoke(_state);
            DebugUtility.LogVerbose<GameLoopManager>("[OBS] GameLoopManager ENTER Playing");
        }

        public void EnterPostGame()
        {
            if (_state == State.PostGame)
            {
                return; // idempotente
            }

            _state = State.PostGame;
            EnteredState?.Invoke(_state);
            DebugUtility.LogVerbose<GameLoopManager>("[OBS] GameLoopManager ENTER PostGame");
        }
    }
}
