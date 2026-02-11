#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    /// <summary>
    /// Adapter para operações de fade desacopladas do legado.
    /// </summary>
    public interface ISceneFlowFadeAdapter
    {
        bool IsAvailable { get; }
        void ConfigureFromProfile(SceneFlowProfileId profileId);
        Task FadeInAsync(string contextSignature = null);
        Task FadeOutAsync(string contextSignature = null);
    }
}
