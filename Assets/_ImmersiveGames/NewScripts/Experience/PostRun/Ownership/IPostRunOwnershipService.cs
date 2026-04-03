namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    /// <summary>
    /// Owner do rail local de PostRun e da transicao semantica para RunDecision.
    /// </summary>
    public interface IPostRunOwnershipService
    {
        bool IsOwnerEnabled { get; }
        bool IsActive { get; }
        bool IsRunDecisionActive { get; }
        void OnPostRunEntered(PostRunOwnershipContext context);
        void OnPostRunCompleted(PostRunOwnershipContext context);
        void OnRunDecisionEntered(PostRunOwnershipContext context);
        void OnPostRunExited(PostRunOwnershipExitContext context);
    }
}

