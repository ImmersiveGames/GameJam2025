// Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/IContentSwapChangeService.cs
#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.ContentSwap
{
    /// <summary>
    /// API de produção para solicitar troca de conteúdo.
    /// - InPlace: conteúdo + hard reset na mesma cena.
    /// </summary>
    public interface IContentSwapChangeService
    {
        Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason);
        Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason, ContentSwapOptions? options);
        Task RequestContentSwapInPlaceAsync(string contentId, string reason, ContentSwapOptions? options = null);
    }
}
