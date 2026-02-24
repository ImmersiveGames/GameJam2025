namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class RuntimePolicyCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "RuntimePolicy";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.RuntimePolicy)
            {
                return;
            }

            context.InstallRuntimePolicy?.Invoke();
            _installed = true;
        }
    }
}
