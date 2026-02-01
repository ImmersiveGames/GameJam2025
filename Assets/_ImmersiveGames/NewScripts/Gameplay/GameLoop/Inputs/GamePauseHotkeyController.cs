using _ImmersiveGames.NewScripts.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;


namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop.Inputs
{
    /// <summary>
    /// Captura ESC e publica eventos NewScripts para pausar/despausar.
    /// Regras:
    /// - Só atua durante uma run ativa (após GameRunStartedEvent).
    /// - Se a run já terminou (GameRunEndedEvent), ESC é ignorado (Victory/Defeat usam overlay próprio).
    /// - Toggle:
    ///     - Se não pausado -> GamePauseCommandEvent(true)
    ///     - Se pausado      -> GameResumeRequestedEvent()
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamePauseHotkeyController : MonoBehaviour
    {
        private bool _runActive;
        private bool _runEnded;
        private bool _paused;

        private EventBinding<GameRunStartedEvent> _onRunStarted;
        private EventBinding<GameRunEndedEvent> _onRunEnded;

        private EventBinding<GamePauseCommandEvent> _onPauseCommand;
        private EventBinding<GameResumeRequestedEvent> _onResumeRequested;
        private EventBinding<GameExitToMenuRequestedEvent> _onExitToMenu;

        private void Awake()
        {
            // Run lifecycle (para diferenciar Pause do jogador vs PostGame)
            _onRunStarted = new EventBinding<GameRunStartedEvent>(_ => OnRunStarted());
            _onRunEnded = new EventBinding<GameRunEndedEvent>(_ => OnRunEnded());

            EventBus<GameRunStartedEvent>.Register(_onRunStarted);
            EventBus<GameRunEndedEvent>.Register(_onRunEnded);

            // Espelha estado local baseado nos próprios eventos do sistema.
            // Isso mantém o toggle robusto mesmo quando pause vem de outra origem.
            _onPauseCommand = new EventBinding<GamePauseCommandEvent>(OnPauseCommand);
            _onResumeRequested = new EventBinding<GameResumeRequestedEvent>(_ => _paused = false);
            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(_ => ResetFlagsForMenu());

            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
        }

        private void OnDestroy()
        {
            EventBus<GameRunStartedEvent>.Unregister(_onRunStarted);
            EventBus<GameRunEndedEvent>.Unregister(_onRunEnded);

            EventBus<GamePauseCommandEvent>.Unregister(_onPauseCommand);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResumeRequested);
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_onExitToMenu);
        }

        private void Update()
        {
            if (!IsEscapePressedThisFrame())
            {
                return;
            }

            // ESC só é Pause do jogador durante run ativa (Gameplay) e antes do final da run.
            if (!_runActive || _runEnded)
            {
                return;
            }

            if (_paused)
            {
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            }
            else
            {
                EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
            }
        }

        private void OnRunStarted()
        {
            _runActive = true;
            _runEnded = false;
            // Não forçamos _paused aqui; ele será mantido pelo espelhamento de eventos.
        }

        private void OnRunEnded()
        {
            _runEnded = true;
            // Em run ended, normalmente o sistema também publica GamePauseCommandEvent(true)
            // (como no seu log), o que vai atualizar _paused via OnPauseCommand.
        }

        private void ResetFlagsForMenu()
        {
            _runActive = false;
            _runEnded = false;
            _paused = false;
        }

        private void OnPauseCommand(GamePauseCommandEvent e)
        {
            // Pelo log você usa GamePauseCommandEvent(true).
            // Assumimos que o evento carrega o bool de pause.
            _paused = e.IsPaused;
        }

        private static bool IsEscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
