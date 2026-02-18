namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class NavigationCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "Navigation";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.Navigation)
            {
                return;
            }

            context.InstallNavigation?.Invoke();
            _installed = true;
        }
    }
}
