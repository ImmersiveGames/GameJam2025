#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Opções para mudança de nível (opções de ContentSwap).
    /// </summary>
    public sealed class LevelChangeOptions
    {
        public ContentSwapOptions? ContentSwapOptions { get; set; }

        public static LevelChangeOptions Default => new LevelChangeOptions();

        public LevelChangeOptions Clone()
        {
            return new LevelChangeOptions
            {
                ContentSwapOptions = ContentSwapOptions?.Clone()
            };
        }
    }
}
