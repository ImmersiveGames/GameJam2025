using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    /// <summary>
    /// Resolve RuntimeModeConfig pelo trilho canÃ´nico:
    /// DI global -> Resources/RuntimeMode/RuntimeModeConfig.
    ///
    /// Observacao:
    /// - Nao consulta BootstrapConfigAsset.
    /// </summary>
    public static class RuntimeModeConfigLoader
    {
        private const string DefaultResourcesPath = "RuntimeMode/RuntimeModeConfig";

        public static RuntimeModeConfig LoadOrNull()
        {
            if (DependencyManager.HasInstance)
            {
                var provider = DependencyManager.Provider;
                if (provider != null && provider.TryGetGlobal<RuntimeModeConfig>(out var config) && config != null)
                {
                    return config;
                }
            }

            return Resources.Load<RuntimeModeConfig>(DefaultResourcesPath);
        }
    }
}

