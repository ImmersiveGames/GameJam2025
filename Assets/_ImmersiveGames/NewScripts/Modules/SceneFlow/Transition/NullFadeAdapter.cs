#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    /// <summary>
    /// Adapter nulo para cenários sem fade configurado.
    /// </summary>
    public sealed class NullFadeAdapter : ISceneFlowFadeAdapter
    {
        public bool IsAvailable => false;

        public void ConfigureFromProfile(SceneFlowProfileId profileId) { }

        public Task FadeInAsync() => Task.CompletedTask;

        public Task FadeOutAsync() => Task.CompletedTask;
    }
}
