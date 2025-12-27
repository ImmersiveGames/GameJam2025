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
    /// Evento definitivo para pausa / despausa.
    /// </summary>
    public sealed class GamePauseCommandEvent : IEvent
    {
        public GamePauseCommandEvent(bool isPaused) => IsPaused = isPaused;
        public bool IsPaused { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent { }
    /// <summary>
    /// REQUEST (intenção): "quero sair do gameplay e voltar ao frontend/menu".
    /// </summary>
    public sealed class GameExitToMenuRequestedEvent : IEvent { }
    public sealed class GameResetRequestedEvent : IEvent { }
}
