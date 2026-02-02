#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels
{
    /// <summary>
    /// Orquestra progressão de níveis, delegando ContentSwap e IntroStageController.
    /// </summary>
    public interface ILevelManager
    {
        Task RequestLevelInPlaceAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null);
    }
}
