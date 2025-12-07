using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
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
    /// Disparado quando todas as cenas alvo foram carregadas,
    /// todas as cenas obsoletas foram descarregadas e a cena ativa
    /// já foi definida. Representa o momento em que o "mundo" está pronto.
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
    /// Representa a conclusão total da transição.
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