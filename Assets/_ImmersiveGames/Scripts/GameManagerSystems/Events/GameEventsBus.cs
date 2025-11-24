using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.GameManagerSystems.Events
{
    public class StateChangedEvent : IEvent
    {
        public readonly bool isGameActive;

        public StateChangedEvent(bool isGameActive)
        {
            this.isGameActive = isGameActive;
        }
    }
    public class GameStartEvent : IEvent
    {
        // Não precisa de dados adicionais, mas pode incluir se necessário
    }

    public class GameStartRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para iniciar uma sessão
    }

    public class GamePauseRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para pausar a sessão
    }

    public class GameResumeRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para retomar após pausa
    }

    public class GameResetRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para reiniciar a sessão
    }

    // Evento disparado para sinalizar o início de um pipeline de reset
    public class GameResetStartedEvent : IEvent
    {
    }

    // Evento disparado após conclusão do reset e recarga de cena
    public class GameResetCompletedEvent : IEvent
    {
    }

    // Evento disparado quando o jogo termina com derrota
    public class GameOverEvent : IEvent
    {
        // Pode incluir dados como motivo do game over, se necessário
    }

    // Evento disparado quando o jogo termina com vitória
    public class GameVictoryEvent : IEvent
    {
        // Pode incluir dados como pontuação, se necessário
    }

    // Evento disparado quando o jogo é pausado ou despausado
    public class GamePauseEvent : IEvent
    {
        public bool IsPaused { get; }

        public GamePauseEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
    public class ActorDeathEvent : IEvent
    {
        public Vector3 Position { get; }
        public IActor Actor { get; }


        public ActorDeathEvent( IActor actor, Vector3 position  )
        {
            Actor = actor;
            Position = position;
        }
    }
    public class ActorReviveEvent : IEvent
    {
        public Vector3 Position { get; }
        public IActor Actor { get; }


        public ActorReviveEvent( IActor actor, Vector3 position  )
        {
            Actor = actor;
            Position = position;
        }
    }
    public class ActorStateChangedEvent : IEvent
    {
        public bool IsActive { get; }

        public ActorStateChangedEvent(bool isActive)
        {
            IsActive = isActive;
        }
    }
}