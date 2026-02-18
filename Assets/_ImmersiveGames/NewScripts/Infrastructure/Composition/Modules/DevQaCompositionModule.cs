namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class DevQaCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "DevQA";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.DevQA)
            {
                return;
            }

            context.InstallDevQa?.Invoke();
            _installed = true;
        }
    }
}
