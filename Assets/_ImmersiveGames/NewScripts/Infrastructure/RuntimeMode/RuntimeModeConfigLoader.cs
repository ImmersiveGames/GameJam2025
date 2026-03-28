using _ImmersiveGames.NewScripts.Core.Composition;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Resolve RuntimeModeConfig apenas a partir do DI global.
    ///
    /// Observação:
    /// - A leitura transitória por Resources foi removida deste helper.
    /// - Bootstrap/composition root deve fazer a resolução explícita quando o asset for obrigatório.
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

            return null;
        }
    }
}
