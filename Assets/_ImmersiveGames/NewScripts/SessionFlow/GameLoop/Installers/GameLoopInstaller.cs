using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Aggregator legado temporario.
    /// Mantido apenas por compatibilidade enquanto os installers nomeados assumem ownership real.
    /// </summary>
    [Obsolete("Use the named installers: GameLoopCoreInstaller, LegacyPauseCompatibilityInstaller, RunPipelineBridgeInstaller, IntroStageIntegrationInstaller, SessionOperationalStartupRouteInstaller.")]
    public static class GameLoopInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            GameLoopCoreInstaller.Install();
            LegacyPauseCompatibilityInstaller.Install();
            RunPipelineBridgeInstaller.Install();
            IntroStageIntegrationInstaller.Install();
            SessionOperationalStartupRouteInstaller.Install();

            _installed = true;

            DebugUtility.Log(typeof(GameLoopInstaller),
                "[OBS][GameLoop][Compatibility] Legacy aggregator concluido. temporary_owner='true'.",
                DebugUtility.Colors.Info);
        }
    }
}
