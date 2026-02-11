#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters
{
    /// <summary>
    /// Adapter nulo para cenários sem fade configurado.
    /// </summary>
    public sealed class NoFadeAdapter : ISceneFlowFadeAdapter
    {
        public bool IsAvailable => false;

        public void ConfigureFromProfile(SceneFlowProfileId profileId) { }

        public Task FadeInAsync(string? contextSignature = null) => Task.CompletedTask;

        public Task FadeOutAsync(string? contextSignature = null) => Task.CompletedTask;
    }
}
