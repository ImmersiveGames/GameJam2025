namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class LevelsCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "Levels";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.Levels)
            {
                return;
            }

            context.InstallLevels?.Invoke();
            _installed = true;
        }
    }
}
