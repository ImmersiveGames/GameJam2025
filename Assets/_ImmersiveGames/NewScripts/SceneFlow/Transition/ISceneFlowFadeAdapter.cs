#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Bindings;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition
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

