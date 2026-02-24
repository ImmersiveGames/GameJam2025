using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
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

    }
}
