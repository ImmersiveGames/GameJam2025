using _ImmersiveGames.Scripts.SceneManagement.Transition;

namespace _ImmersiveGames.Scripts.SceneManagement.Hud
{
    /// <summary>
    /// Serviço para exibir/ocultar um overlay de loading durante transições de cena.
    /// Implementações típicas vão reagir aos eventos de SceneTransitionService,
    /// mas a interface permite que outros sistemas também controlem o overlay se necessário.
    /// </summary>
    public interface ISceneLoadingHudService
    {
        void ShowLoading(SceneTransitionContext context);
        void MarkScenesReady(SceneTransitionContext context);
        void HideLoading(SceneTransitionContext context);
    }
}