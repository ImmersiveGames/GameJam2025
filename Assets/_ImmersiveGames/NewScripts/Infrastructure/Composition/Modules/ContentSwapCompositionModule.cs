namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class ContentSwapCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "ContentSwap";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.ContentSwap)
            {
                return;
            }

            context.InstallContentSwap?.Invoke();
            _installed = true;
        }
    }
}
