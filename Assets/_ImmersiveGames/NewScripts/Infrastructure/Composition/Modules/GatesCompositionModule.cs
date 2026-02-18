namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class GatesCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "Gates";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.Gates)
            {
                return;
            }

            context.InstallGates?.Invoke();
            _installed = true;
        }
    }
}
