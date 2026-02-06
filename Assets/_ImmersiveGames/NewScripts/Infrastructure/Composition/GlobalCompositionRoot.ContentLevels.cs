using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.Levels;
using _ImmersiveGames.NewScripts.Modules.Levels.Dev;
using _ImmersiveGames.NewScripts.Modules.Levels.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterContentSwapChangeService()
        {
            var provider = DependencyManager.Provider;

            if (provider.TryGetGlobal<IContentSwapChangeService>(out var existing) && existing != null)
            {
                return;
            }

            if (!provider.TryGetGlobal<IContentSwapContextService>(out var contextService) || contextService == null)
            {
                throw new InvalidOperationException(
                    "IContentSwapContextService is not registered. Ensure GlobalCompositionRoot registered it before ContentSwapChangeService.");
            }

            provider.RegisterGlobal<IContentSwapChangeService>(new InPlaceContentSwapService(contextService));
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                "[ContentSwap] Registered IContentSwapChangeService (InPlaceOnly).",
                DebugUtility.Colors.Success);
        }

        private static void RegisterLevelServices()
        {
            var provider = DependencyManager.Provider;

            if (provider.TryGetGlobal<ILevelManager>(out var existing) && existing != null)
            {
                return;
            }

            LevelManagerInstaller.EnsureRegistered(fromBootstrap: true);
        }

        private static void RegisterLevelQaInstaller()
        {
            try
            {
                LevelDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][Level] Falha ao instalar LevelDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

    }
}
