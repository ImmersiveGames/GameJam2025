namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    /// <summary>
    /// Owner do input/gate do PostGame, evitando duplicidade com overlays.
    /// </summary>
    public interface IPostGameOwnershipService
    {
        bool IsOwnerEnabled { get; }
        void OnPostGameEntered(PostGameOwnershipContext context);
        void OnPostGameExited(PostGameOwnershipExitContext context);
    }
}
