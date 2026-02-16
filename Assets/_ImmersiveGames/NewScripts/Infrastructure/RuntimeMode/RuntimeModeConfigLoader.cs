using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Resolve RuntimeModeConfig via DI global e fallback por Resources (path canônico).
    /// Retorna null quando não houver configuração disponível.
    /// </summary>
    public static class RuntimeModeConfigLoader
    {
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

            RuntimeModeConfig fromResources = Resources.Load<RuntimeModeConfig>(RuntimeModeConfig.DefaultResourcesPath);
            return fromResources;
        }
    }
}
