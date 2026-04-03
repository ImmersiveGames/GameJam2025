using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Bootstrap
{
    /// <summary>
    /// Installer do Navigation.
    ///
    /// Responsabilidade:
    /// - registrar contratos, servicos e configuracoes do modulo no boot;
    /// - nao compor runtime nem ativar bridges/coordinators.
    /// </summary>
    public static class NavigationInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] BootstrapConfigAsset obrigatorio ausente para instalar Navigation.");
            }

            RegisterNavigationCatalog(bootstrapConfig);

            _installed = true;

            DebugUtility.Log(typeof(NavigationInstaller),
                "[Navigation][Core] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterNavigationCatalog(BootstrapConfigAsset bootstrapConfig)
        {
            var catalogAsset = bootstrapConfig.NavigationCatalog;
            if (catalogAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] Missing required BootstrapConfigAsset.navigationCatalog.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var existingCatalog) || existingCatalog == null)
            {
                DependencyManager.Provider.RegisterGlobal<IGameNavigationCatalog>(catalogAsset);

                catalogAsset.GetObservabilitySnapshot(out int rawRoutesCount, out int builtRouteIdsCount, out bool hasToGameplay);
                DebugUtility.LogVerbose(typeof(NavigationInstaller),
                    $"[OBS][Navigation][Core] CatalogResolvedVia=BootstrapConfig field=navigationCatalog asset={catalogAsset.name} rawRoutesCount={rawRoutesCount} builtRouteIdsCount={builtRouteIdsCount} hasToGameplay={hasToGameplay}.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingCatalog, catalogAsset))
            {
                string diAssetName = existingCatalog is UnityEngine.Object diObject ? diObject.name : existingCatalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][Navigation] GameNavigationCatalog mismatch: DI has {diAssetName} but BootstrapConfig has {catalogAsset.name}.");
            }
        }
    }
}
