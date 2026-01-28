#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    public interface IContentSwapChangeServiceCapabilities
    {
        bool SupportsWithTransition { get; }
        string CapabilityReason { get; }
    }
}
