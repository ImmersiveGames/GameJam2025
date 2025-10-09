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