#nullable enable
using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Fade.Runtime
{

    public interface IFadeService
    {
        Task EnsureReadyAsync();
        void Configure(FadeConfig config);
        Task FadeInAsync(string? contextSignature = null);
        Task FadeOutAsync(string? contextSignature = null);
    }
}

