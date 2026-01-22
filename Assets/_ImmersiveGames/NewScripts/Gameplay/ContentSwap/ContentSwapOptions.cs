#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Opções de execução para troca de conteúdo (fade, HUD e timeout).
    /// </summary>
    public sealed class ContentSwapOptions
    {
        public const int DefaultTimeoutMs = 20000;

        public bool UseFade { get; set; }
        public bool UseLoadingHud { get; set; }
        public int TimeoutMs { get; set; } = DefaultTimeoutMs;

        public static ContentSwapOptions Default => new ContentSwapOptions();

        public ContentSwapOptions Clone()
        {
            return new ContentSwapOptions
            {
                UseFade = UseFade,
                UseLoadingHud = UseLoadingHud,
                TimeoutMs = TimeoutMs
            };
        }
    }
}
