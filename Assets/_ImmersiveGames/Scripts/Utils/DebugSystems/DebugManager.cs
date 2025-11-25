using _ImmersiveGames.Scripts.GameManagerSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.DebugSystems
{
    /// <summary>
    /// Responsável por aplicar configurações globais do <see cref="DebugUtility"/>.
    /// Mantém as escolhas centralizadas para que o GameManager apenas solicite a aplicação.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DebugManager : MonoBehaviour
    {
        [Header("Lifecycle")]
        [SerializeField]
        private bool applyOnAwake = true;

        [Header("Global Flags")]
        [SerializeField]
        private bool globalDebugEnabled = true;

        [SerializeField]
        private bool verboseInEditor = true;

        [SerializeField]
        private bool verboseInPlayer;

        [SerializeField]
        private bool fallbacksInEditor = true;

        [SerializeField]
        private bool fallbacksInPlayer;

        [SerializeField]
        private bool repeatedCallVerboseInEditor = true;

        [SerializeField]
        private bool repeatedCallVerboseInPlayer;

        [Header("Default Levels")]
        [SerializeField]
        private DebugLevel editorDefaultLevel = DebugLevel.Logs;

        [SerializeField]
        private DebugLevel playerDefaultLevel = DebugLevel.Warning;

        [Header("Game Config Overrides")]
        [SerializeField]
        private bool honorGameConfigDebugMode = true;

        [SerializeField]
        private DebugLevel debugModeLevel = DebugLevel.Verbose;

        private void Awake()
        {
            if (!applyOnAwake)
            {
                return;
            }

            ApplyConfiguration();
        }

        /// <summary>
        /// Aplica as configurações padrão sem considerar uma <see cref="GameConfig"/> específica.
        /// </summary>
        [ContextMenu("Debug/Apply Configuration")]
        public void ApplyConfiguration()
        {
            ApplyConfigurationInternal(null);
        }

        /// <summary>
        /// Aplica as configurações levando em conta uma instância de <see cref="GameConfig"/>.
        /// </summary>
        /// <param name="config">Configuração do jogo que pode sinalizar modo debug.</param>
        public void ApplyConfiguration(GameConfig config)
        {
            ApplyConfigurationInternal(config);
        }

        private void ApplyConfigurationInternal(GameConfig config)
        {
            bool verboseEnabled = Application.isEditor ? verboseInEditor : verboseInPlayer;
            bool fallbacksEnabled = Application.isEditor ? fallbacksInEditor : fallbacksInPlayer;
            bool repeatedCallVerbose = Application.isEditor ? repeatedCallVerboseInEditor : repeatedCallVerboseInPlayer;
            DebugLevel defaultLevel = Application.isEditor ? editorDefaultLevel : playerDefaultLevel;

            if (honorGameConfigDebugMode && config != null && config.DebugMode)
            {
                verboseEnabled = true;
                fallbacksEnabled = true;
                repeatedCallVerbose = true;
                defaultLevel = debugModeLevel;
            }

            DebugUtility.SetGlobalDebugState(globalDebugEnabled);
            DebugUtility.SetVerboseLogging(verboseEnabled);
            DebugUtility.SetLogFallbacks(fallbacksEnabled);
            DebugUtility.SetRepeatedCallVerbose(repeatedCallVerbose);
            DebugUtility.SetDefaultDebugLevel(defaultLevel);

            DebugUtility.LogVerbose<DebugManager>(
                $"Configuração de debug aplicada (Level: {defaultLevel}, Verbose: {verboseEnabled}, Fallbacks: {fallbacksEnabled}, Repeated: {repeatedCallVerbose}).",
                DebugUtility.Colors.CrucialInfo);
        }
    }
}
