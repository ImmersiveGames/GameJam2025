using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// LEGACY: listener desativado.
    /// O owner can?nico de GameExitToMenuRequestedEvent ? ExitToMenuCoordinator.
    /// </summary>
    public sealed class ExitToMenuNavigationBridge : IDisposable
    {
        private bool _disposed;

        public ExitToMenuNavigationBridge()
        {
            DebugUtility.LogVerbose<ExitToMenuNavigationBridge>(
                "[OBS][LEGACY] ExitToMenuNavigationBridge disabled; ExitToMenuCoordinator owns canonical exit.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }
}
