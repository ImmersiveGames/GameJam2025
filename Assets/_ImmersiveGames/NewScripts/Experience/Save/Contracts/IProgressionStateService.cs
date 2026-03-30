namespace _ImmersiveGames.NewScripts.Modules.Save.Contracts
{
    public interface IProgressionStateService
    {
        bool HasSnapshot { get; }

        ProgressionSnapshot CurrentSnapshot { get; }

        void SetCurrent(
            ProgressionSnapshot snapshot,
            string reason);
    }
}
