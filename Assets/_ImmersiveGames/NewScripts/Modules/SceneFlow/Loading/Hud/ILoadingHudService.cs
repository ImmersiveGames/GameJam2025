using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Hud
{
    /// <summary>
    /// Serviço mínimo para o HUD de loading do Scene Flow (NewScripts).
    ///
    /// Contrato:
    /// - EnsureLoadedAsync deve garantir que a cena/Controller existam (ou degradar em Release).
    /// - Show/Hide são idempotentes e usam signature+phase para correlacionar evidência em logs.
    /// </summary>
    public interface ILoadingHudService
    {
        Task EnsureLoadedAsync(string signature);
        void Show(string signature, string phase);
        void Hide(string signature, string phase);
    }
}
