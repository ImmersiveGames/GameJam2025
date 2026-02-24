namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Contrato para catálogos de estilo de transição.
    /// </summary>
    public interface ITransitionStyleCatalog
    {
        bool TryGet(TransitionStyleId styleId, out TransitionStyleDefinition style);
    }
}
