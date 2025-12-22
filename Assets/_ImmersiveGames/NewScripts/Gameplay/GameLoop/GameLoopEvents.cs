using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Evento definitivo indicando que o loop de jogo deve iniciar.
    /// </summary>
    public sealed class GameStartEvent : IEvent { }

    /// <summary>
    /// Evento definitivo indicando o estado atual de pausa.
    /// </summary>
    public sealed class GamePauseEvent : IEvent
    {
        public bool IsPaused { get; set; }

        public GamePauseEvent() { }

        public GamePauseEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    /// <summary>
    /// Evento solicitando que o jogo seja retomado após um pause.
    /// </summary>
    public sealed class GameResumeRequestedEvent : IEvent { }

    /// <summary>
    /// Evento solicitando reset completo do loop de jogo.
    /// </summary>
    public sealed class GameResetRequestedEvent : IEvent { }
}
