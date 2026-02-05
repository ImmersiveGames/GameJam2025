using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Carrega o RuntimeModeConfig via Resources.
    ///
    /// Observação:
    /// - O asset deve existir em: Assets/_ImmersiveGames/NewScripts/Resources/RuntimeModeConfig.asset
    /// - Se não existir, retorna null e o sistema segue em modo Auto (comportamento atual).
    /// </summary>
    public static class RuntimeModeConfigLoader
    {
        public const string DefaultResourceName = "RuntimeModeConfig";

        public static RuntimeModeConfig LoadOrNull()
        {
            return Resources.Load<RuntimeModeConfig>(DefaultResourceName);
        }
    }
}
