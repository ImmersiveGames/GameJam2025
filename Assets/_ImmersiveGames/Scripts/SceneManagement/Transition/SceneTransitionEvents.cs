using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Disparado assim que o OldSceneTransitionService inicia o processamento
    /// de uma transi��o (antes de FadeIn, Load ou Unload).
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
    /// j� foi definida. Representa o momento em que o "mundo" est� pronto.
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
    /// Disparado ao final da transi��o, depois do FadeOut (quando usado).
    /// Representa a conclus�o total da transi��o.
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

