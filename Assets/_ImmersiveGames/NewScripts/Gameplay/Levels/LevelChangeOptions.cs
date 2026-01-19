#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Opções para mudança de nível (modo de ContentSwap, request de transição e opções de fase).
    /// </summary>
    public sealed class LevelChangeOptions
    {
        public PhaseChangeMode Mode { get; set; } = PhaseChangeMode.InPlace;
        public SceneTransitionRequest? TransitionRequest { get; set; }
        public PhaseChangeOptions? PhaseOptions { get; set; }

        public static LevelChangeOptions Default => new LevelChangeOptions();

        public LevelChangeOptions Clone()
        {
            return new LevelChangeOptions
            {
                Mode = Mode,
                TransitionRequest = TransitionRequest,
                PhaseOptions = PhaseOptions?.Clone()
            };
        }
    }
}
