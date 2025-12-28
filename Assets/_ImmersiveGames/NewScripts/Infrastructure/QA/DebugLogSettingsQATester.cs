#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Execução manual para validar a aplicação das configurações do DebugUtility.
    /// </summary>
    public sealed class DebugLogSettingsQaTester : MonoBehaviour
    {
        [SerializeField] private NewDebugManager debugManager;

        [ContextMenu("QA/DebugLog/Run")]
        public void Run()
        {
            ApplySettings();
            DumpCurrentState();
            EmitLevelSamples();
            TestDeduplication();
            DebugUtility.Log(typeof(DebugLogSettingsQaTester), "[QA][DebugLog] QA complete.");
        }

        private void ApplySettings()
        {
            if (debugManager != null)
            {
                debugManager.ApplyConfiguration();
                return;
            }

            DebugUtility.LogWarning(typeof(DebugLogSettingsQaTester),
                "[QA][DebugLog] NewDebugManager não definido; usando estado atual do DebugUtility.");
        }

        private void DumpCurrentState()
        {
            DebugUtility.Log(typeof(DebugLogSettingsQaTester),
                $"[QA][DebugLog] Estado => Global={DebugUtility.IsGlobalDebugEnabled}, Verbose={DebugUtility.IsVerboseLoggingEnabled}, Fallbacks={DebugUtility.IsFallbacksEnabled}, RepeatedVerbose={DebugUtility.IsRepeatedCallVerboseEnabled}, DefaultLevel={DebugUtility.DefaultDebugLevel}.");
        }

        private void EmitLevelSamples()
        {
            DebugUtility.LogError(typeof(DebugLogSettingsQaTester), "[QA][DebugLog] Sample Error");
            DebugUtility.LogWarning(typeof(DebugLogSettingsQaTester), "[QA][DebugLog] Sample Warning");
            DebugUtility.Log(typeof(DebugLogSettingsQaTester), "[QA][DebugLog] Sample Log");
            DebugUtility.LogVerbose(typeof(DebugLogSettingsQaTester), "[QA][DebugLog] Sample Verbose");
        }

        private void TestDeduplication()
        {
            DebugUtility.Log(typeof(DebugLogSettingsQaTester),
                "[QA][DebugLog] Teste de dedupe: duas chamadas verbose com deduplicate=true no mesmo frame (espera 1 log).");

            DebugUtility.LogVerbose(typeof(DebugLogSettingsQaTester),
                "[QA][DebugLog] Dedupe frame check", deduplicate: true);
            DebugUtility.LogVerbose(typeof(DebugLogSettingsQaTester),
                "[QA][DebugLog] Dedupe frame check", deduplicate: true);
        }
    }
}

#endif
