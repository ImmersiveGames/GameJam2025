namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Implementação default permissiva para validação de navegação.
    /// </summary>
    public sealed class AllowAllNavigationPolicy : INavigationPolicy
    {
        public bool CanTransition(SceneTransitionRequest request, out string denialReason)
        {
            denialReason = string.Empty;
            return true;
        }
    }
}
