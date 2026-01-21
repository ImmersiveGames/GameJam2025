#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Orquestra progressão de níveis, delegando ContentSwap (Phase) e IntroStage.
    /// </summary>
    public interface ILevelManager
    {
        Task RequestLevelInPlaceAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null);
        Task RequestLevelWithTransitionAsync(LevelPlan plan, SceneTransitionRequest transition, string reason, LevelChangeOptions? options = null);
    }
}
