namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    /// <summary>
    /// Owner do input/gate do PostRun, evitando duplicidade com overlays.
    /// </summary>
    public interface IPostRunOwnershipService
    {
        bool IsOwnerEnabled { get; }
        bool IsActive { get; }
        void OnPostRunEntered(PostRunOwnershipContext context);
        void OnPostRunExited(PostRunOwnershipExitContext context);
    }
}

