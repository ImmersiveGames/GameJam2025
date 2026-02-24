namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class SceneFlowCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "SceneFlow";

        public void Install(GlobalCompositionContext context)
        {
            if (context == null || context.Stage != CompositionInstallStage.SceneFlow)
            {
                return;
            }

            if (_installed)
            {
                return;
            }

            context.InstallSceneFlow?.Invoke();
            _installed = true;
        }
    }
}
