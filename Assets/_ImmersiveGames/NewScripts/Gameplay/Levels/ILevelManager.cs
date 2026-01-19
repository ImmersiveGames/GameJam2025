#nullable enable
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Orquestra progressão de níveis, delegando ContentSwap (Phase) e IntroStage.
    /// </summary>
    public interface ILevelManager
    {
        Task GoToLevelAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null);
        Task AdvanceAsync(string reason, LevelChangeOptions? options = null);
        Task BackAsync(string reason, LevelChangeOptions? options = null);
        Task RestartLevelAsync(string reason, LevelChangeOptions? options = null);
    }
}
