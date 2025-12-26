namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Interface simples para HUD de loading do Scene Flow.
    /// </summary>
    public interface ISceneLoadingHud
    {
        void Show(string title, string details = null);
        void SetProgress01(float progress01);
        void Hide();
        bool IsVisible { get; }
    }
}
