using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public sealed class RestartNavigationBridge : IDisposable
    {
        public RestartNavigationBridge()
        {
            DebugUtility.LogWarning<RestartNavigationBridge>(
                "[WARN][LEGACY_API_USED][Navigation] RestartNavigationBridge is disabled. MacroRestartCoordinator owns canonical GameResetRequestedEvent handling.");
        }

        public void Dispose()
        {
        }
    }
}
