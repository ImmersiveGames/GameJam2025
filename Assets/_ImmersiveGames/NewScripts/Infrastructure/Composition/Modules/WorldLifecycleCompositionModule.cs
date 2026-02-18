namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class WorldLifecycleCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "WorldLifecycle";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.WorldLifecycle)
            {
                return;
            }

            context.InstallWorldLifecycle?.Invoke();
            _installed = true;
        }
    }
}
