using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.Levels;
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
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[LevelFlow] ERRO: ILevelManager encontrado no DI global. Levels legacy não pode coexistir com LevelFlow.");
                throw new InvalidOperationException(
                    "Levels legacy detectado. Remova o registro de ILevelManager e use LevelFlow (LevelCatalogAsset).");
            }

            DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                "[LevelFlow] Levels legacy desativado. Use LevelFlow (LevelCatalogAsset/ILevelFlowService) para seleção de níveis.");
        }

        private static void RegisterLevelQaInstaller()
        {
            DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                "[QA][Level] LevelDevContextMenu desativado junto com Levels legacy. Use ferramentas de LevelFlow.");
        }

    }
}
