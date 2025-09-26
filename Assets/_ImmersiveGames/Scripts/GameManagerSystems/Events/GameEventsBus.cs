using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.GameManagerSystems.Events
{
    public class StateChangedEvent : IEvent
    {
        public readonly bool IsGameActive;

        public StateChangedEvent(bool isGameActive)
        {
            IsGameActive = isGameActive;
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
    public class DeathEvent : IEvent
    {
        public Vector3? Position { get; }
        public GameObject SourceGameObject{ get; }
        public string ActorId { get; set; }


        public DeathEvent(Vector3 position, GameObject source)
        {
            Position = position;
            SourceGameObject = source;
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