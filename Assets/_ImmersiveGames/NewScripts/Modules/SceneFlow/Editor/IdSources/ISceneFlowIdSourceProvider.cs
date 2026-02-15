namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Contrato genérico para providers de IDs tipados no editor.
    /// </summary>
    public interface ISceneFlowIdSourceProvider<TId>
    {
        SceneFlowIdSourceResult Collect();
    }
}
