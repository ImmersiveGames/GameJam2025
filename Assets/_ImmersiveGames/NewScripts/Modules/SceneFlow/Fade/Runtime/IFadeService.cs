using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime
{

    public interface IFadeService
    {
        Task EnsureReadyAsync();
        void Configure(FadeConfig config);
        Task FadeInAsync(string? contextSignature = null);
        Task FadeOutAsync(string? contextSignature = null);
    }
}
