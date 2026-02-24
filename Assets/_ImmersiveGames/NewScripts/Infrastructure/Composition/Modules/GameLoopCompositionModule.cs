namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public sealed class GameLoopCompositionModule : IGlobalCompositionModule
    {
        private static bool _installed;

        public string Name => "GameLoop";

        public void Install(GlobalCompositionContext context)
        {
            if (_installed || context == null || context.Stage != CompositionInstallStage.GameLoop)
            {
                return;
            }

            context.InstallGameLoop?.Invoke();
            _installed = true;
        }
    }
}
