namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço responsável por garantir a HUD de loading (LoadingHudScene) e aplicar estado visível/oculto.
    /// </summary>
    public interface INewScriptsLoadingHudService
    {
        void Show(string title, string details, float progress01);
        void SetProgress01(float progress01);
        void Hide();
    }
}
