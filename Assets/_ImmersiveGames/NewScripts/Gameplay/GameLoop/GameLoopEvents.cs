using System;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// REQUEST (intenção): "quero iniciar a simulação".
    /// O start definitivo é o COMMAND executado pelo Coordinator via IGameLoopService.RequestStart() quando ready.
    /// </summary>
    public sealed class GameStartRequestedEvent : IEvent { }

    /// <summary>
    /// Compatibilidade: nome legado que alguns chamadores adotaram.
    /// Apesar do nome "Command", este evento é tratado como REQUEST (intenção).
    /// </summary>
    [Obsolete("Use GameStartRequestedEvent. Este evento é um alias legado tratado como REQUEST.")]
    public sealed class GameStartCommandEvent : IEvent { }

    /// <summary>
    /// Evento definitivo para pausa / despausa.
    /// </summary>
    public sealed class GamePauseCommandEvent : IEvent
    {
        public GamePauseCommandEvent(bool isPaused) => IsPaused = isPaused;
        public bool IsPaused { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent { }
    public sealed class GameResetRequestedEvent : IEvent { }
}
