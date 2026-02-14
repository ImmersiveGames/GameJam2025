#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.DebugLog
{
    /// <summary>
    /// Aplica configurações do DebugUtility a partir de um DebugLogSettings serializado.
    /// </summary>
    public sealed class NewDebugManager : MonoBehaviour
    {
        [SerializeField] private DebugLogSettings settings;
        [SerializeField] private bool applyOnAwake = true;

        private void Awake()
        {
            if (applyOnAwake)
            {
                ApplyConfiguration();
            }
        }

        [ContextMenu("Dev/Debug/Apply Settings")]
        public void ApplyConfiguration()
        {
            if (settings == null)
            {
                Debug.LogWarning("[NewDebugManager] DebugLogSettings não atribuído; aplicação ignorada.");
                return;
            }

            bool isEditor = Application.isEditor;

            bool globalEnabled = settings.globalDebugEnabled;
            bool verboseEnabled = isEditor ? settings.verboseInEditor : settings.verboseInPlayer;
            bool fallbacksEnabled = isEditor ? settings.fallbacksInEditor : settings.fallbacksInPlayer;
            bool repeatedVerboseEnabled = isEditor
                ? settings.repeatedCallVerboseInEditor
                : settings.repeatedCallVerboseInPlayer;
            DebugLevel defaultLevel = isEditor ? settings.editorDefaultLevel : settings.playerDefaultLevel;

            if (settings.forceDebugMode)
            {
                globalEnabled = true;
                verboseEnabled = true;
                fallbacksEnabled = true;
                repeatedVerboseEnabled = true;
                defaultLevel = settings.debugModeLevel;
            }

            DebugUtility.SetGlobalDebugState(globalEnabled);
            DebugUtility.SetVerboseLogging(verboseEnabled);
            DebugUtility.SetLogFallbacks(fallbacksEnabled);
            DebugUtility.SetRepeatedCallVerbose(repeatedVerboseEnabled);
            DebugUtility.SetDefaultDebugLevel(defaultLevel);

            DebugUtility.Log(typeof(NewDebugManager),
                $"[NewDebugManager] Settings aplicados (Editor={isEditor}). Global={globalEnabled}, Verbose={verboseEnabled}, Fallbacks={fallbacksEnabled}, RepeatedVerbose={repeatedVerboseEnabled}, DefaultLevel={defaultLevel}.");
        }

        [ContextMenu("Dev/Debug/Print Current Debug State")]
        public void PrintCurrentDebugState()
        {
            DebugUtility.Log(typeof(NewDebugManager),
                $"[NewDebugManager] Estado atual => Global={DebugUtility.IsGlobalDebugEnabled}, Verbose={DebugUtility.IsVerboseLoggingEnabled}, Fallbacks={DebugUtility.IsFallbacksEnabled}, RepeatedVerbose={DebugUtility.IsRepeatedCallVerboseEnabled}, DefaultLevel={DebugUtility.DefaultDebugLevel}.");
        }
    }
}

#endif
