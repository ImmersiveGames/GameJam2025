using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Resolve o RuntimeModeConfig via DI global (sem Resources).
    ///
    /// Observação:
    /// - A config é opcional neste fluxo; se não estiver registrada no DI, retorna null
    ///   e o sistema segue em modo Auto (comportamento atual).
    /// - O bootstrap pode registrar NewScriptsBootstrapConfigAsset no DI; este loader valida
    ///   esse cenário para manter alinhamento com a estratégia BootstrapConfig-first.
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

            if (provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeConfig) && runtimeConfig != null)
            {
                return runtimeConfig;
            }

            // BootstrapConfig-first (via DI): hoje o bootstrap root não expõe RuntimeModeConfig
            // diretamente; mantemos a checagem para diagnóstico/ordem de resolução futura sem
            // reintroduzir path-based loading.
            if (provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var bootstrapConfig) &&
                bootstrapConfig != null &&
                provider.TryGetGlobal<RuntimeModeConfig>(out runtimeConfig) &&
                runtimeConfig != null)
            {
                return runtimeConfig;
            }

            return null;
        }
    }
}
