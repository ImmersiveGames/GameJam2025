namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Resolução opcional de conteúdo associado ao LevelId (compat observability).
    /// </summary>
    public interface ILevelContentResolver
    {
        bool TryResolveContentId(LevelId levelId, out string contentId);
    }
}
