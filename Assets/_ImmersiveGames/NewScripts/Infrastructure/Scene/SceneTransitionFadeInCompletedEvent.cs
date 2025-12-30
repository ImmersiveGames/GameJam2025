namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Emitido pelo SceneTransitionService após a etapa de FadeIn (quando UseFade=true),
    /// antes das operações de Load/Unload/ActiveScene. Usado para habilitar a Opção A+
    /// do Loading HUD: mostrar apenas quando a tela já está escura.
    /// </summary>
    public readonly struct SceneTransitionFadeInCompletedEvent
    {
        public SceneTransitionFadeInCompletedEvent(SceneTransitionContext context)
        {
            Context = context;
        }

        public SceneTransitionContext Context { get; }
    }
}
