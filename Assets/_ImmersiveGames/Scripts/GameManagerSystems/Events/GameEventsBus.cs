using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using GameStartEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameStartEvent;
using GamePauseEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GamePauseEvent;
using GameResumeRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResumeRequestedEvent;
using GameResetRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResetRequestedEvent;

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
    public class GameStartRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para iniciar uma sessão
    }

    public class GamePauseRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para pausar a sessão
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
    public class GameReturnToMenuRequestedEvent : IEvent
    {
    }
}
