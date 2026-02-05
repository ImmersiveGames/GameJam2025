#nullable enable
namespace _ImmersiveGames.NewScripts.Modules.Levels
{
    public interface ILevelSessionService
    {
        string SelectedLevelId { get; }
        LevelPlan SelectedPlan { get; }
        string AppliedLevelId { get; }
        LevelPlan AppliedPlan { get; }

        bool Initialize();
        bool SelectInitial(string reason);
        bool SelectLevelById(string levelId, string reason);
        bool SelectNext(string reason);
        bool SelectPrevious(string reason);
        bool ApplySelected(string reason);
    }
}
