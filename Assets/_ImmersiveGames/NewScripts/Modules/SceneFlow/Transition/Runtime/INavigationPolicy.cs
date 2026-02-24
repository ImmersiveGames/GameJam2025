namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Política de navegação usada para validar transições antes do processamento do SceneFlow.
    /// </summary>
    public interface INavigationPolicy
    {
        bool CanTransition(SceneTransitionRequest request, out string denialReason);
    }
}
