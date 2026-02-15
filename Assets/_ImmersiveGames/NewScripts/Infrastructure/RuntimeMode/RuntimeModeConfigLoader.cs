using _ImmersiveGames.NewScripts.Core.Composition;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Resolve RuntimeModeConfig apenas via DI global.
    /// Se não houver configuração registrada, retorna null.
    /// </summary>
    public static class RuntimeModeConfigLoader
    {
        public static RuntimeModeConfig LoadOrNull()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return null;
            }

            return provider.TryGetGlobal<RuntimeModeConfig>(out var config)
                ? config
                : null;
        }
    }
}
