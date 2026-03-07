#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime
{
    public static class ContentSwapDevBootstrapper
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void EnsureInstalled()
        {
            DebugUtility.LogVerbose(typeof(ContentSwapDevBootstrapper),
                "[OBS][LEGACY][DevQA] ContentSwapDevBootstrapper.EnsureInstalled called; canonical owner is GlobalCompositionRoot.DevQA.",
                DebugUtility.Colors.Info);

            ContentSwapDevInstaller.EnsureInstalled();
        }
#endif
    }
}