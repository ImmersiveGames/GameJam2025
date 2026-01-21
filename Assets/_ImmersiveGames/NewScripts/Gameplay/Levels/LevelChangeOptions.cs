#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Phases;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Opções para mudança de nível (opções de ContentSwap).
    /// </summary>
    public sealed class LevelChangeOptions
    {
        public PhaseChangeOptions? PhaseOptions { get; set; }

        public static LevelChangeOptions Default => new LevelChangeOptions();

        public LevelChangeOptions Clone()
        {
            return new LevelChangeOptions
            {
                PhaseOptions = PhaseOptions?.Clone()
            };
        }
    }
}
