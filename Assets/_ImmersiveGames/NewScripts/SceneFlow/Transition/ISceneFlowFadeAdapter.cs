#nullable enable
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Bindings;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition
{
    /// <summary>
    /// Adapter para operações de fade desacopladas do legado.
    /// </summary>
    public interface ISceneFlowFadeAdapter
    {
        bool IsAvailable { get; }
        void ConfigureFromProfile(SceneTransitionProfile profile, string profileLabel);
        Task FadeInAsync(string? contextSignature = null);
        Task FadeOutAsync(string? contextSignature = null);
    }
}

