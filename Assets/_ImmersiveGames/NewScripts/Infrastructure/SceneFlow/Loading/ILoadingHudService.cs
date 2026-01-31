using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço mínimo para o HUD de loading do Scene Flow (NewScripts).
    /// </summary>
    public interface ILoadingHudService
    {
        Task EnsureLoadedAsync();
        void Show(string signature, string phase);
        void Hide(string signature, string phase);
    }
}
