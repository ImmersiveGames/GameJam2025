#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Opções de execução para troca de fase (fade, HUD e timeout).
    /// </summary>
    public sealed class PhaseChangeOptions
    {
        public const int DefaultTimeoutMs = 20000;

        public bool UseFade { get; set; }
        public bool UseLoadingHud { get; set; }
        public int TimeoutMs { get; set; } = DefaultTimeoutMs;

        public static PhaseChangeOptions Default => new PhaseChangeOptions();

        public PhaseChangeOptions Clone()
        {
            return new PhaseChangeOptions
            {
                UseFade = UseFade,
                UseLoadingHud = UseLoadingHud,
                TimeoutMs = TimeoutMs
            };
        }
    }
}
