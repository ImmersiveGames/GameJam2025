using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.Scripts.ActorSystems;
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
        // N�o precisa de dados adicionais, mas pode incluir se necess�rio
    }

    public class OldGameStartRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para iniciar uma sess�o
    }

    public class GamePauseRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para pausar a sess�o
    }

    public class OldGameResumeRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para retomar ap�s pausa
    }

    public class OldGameResetRequestedEvent : IEvent
    {
        // Solicitado por UI/controles para reiniciar a sess�o
    }

    // Evento disparado para sinalizar o in�cio de um pipeline de reset
    public class GameResetStartedEvent : IEvent
    {
    }

    // Evento disparado ap�s conclus�o do reset e recarga de cena
    public class GameResetCompletedEvent : IEvent
    {
    }

    // Evento disparado quando o jogo termina com derrota
    public class GameOverEvent : IEvent
    {
        // Pode incluir dados como motivo do game over, se necess�rio
    }

    // Evento disparado quando o jogo termina com vit�ria
    public class GameVictoryEvent : IEvent
    {
        // Pode incluir dados como pontua��o, se necess�rio
    }

    // Evento disparado quando o jogo � pausado ou despausado
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
    public class GameReturnToMenuRequestedEvent : IEvent
    {
    }
}
