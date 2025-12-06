using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SceneManagement.Events
{
    /// <summary>
    /// Disparado assim que o SceneTransitionService inicia o processamento
    /// de uma transição (antes de FadeIn, Load ou Unload).
    /// </summary>
    public readonly struct SceneTransitionStartedEvent : IEvent
    {
        public SceneTransitionContext Context { get; }

        public SceneTransitionStartedEvent(SceneTransitionContext context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// Disparado quando todas as cenas alvo já foram carregadas,
    /// a cena ativa já foi definida e todas as cenas obsoletas
    /// já foram descarregadas.
    ///
    /// Ou seja, o "estado de cena" já está pronto; o que falta,
    /// se houver, é apenas completar o FadeOut.
    /// </summary>
    public readonly struct SceneTransitionScenesReadyEvent : IEvent
    {
        public SceneTransitionContext Context { get; }

        public SceneTransitionScenesReadyEvent(SceneTransitionContext context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// Disparado ao final da transição, depois do FadeOut (quando usado).
    /// </summary>
    public readonly struct SceneTransitionCompletedEvent : IEvent
    {
        public SceneTransitionContext Context { get; }

        public SceneTransitionCompletedEvent(SceneTransitionContext context)
        {
            Context = context;
        }
    }
}