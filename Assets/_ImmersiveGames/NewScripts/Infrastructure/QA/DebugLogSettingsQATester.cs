using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Execução manual para validar a aplicação das configurações do DebugUtility.
    /// </summary>
    public sealed class DebugLogSettingsQATester : MonoBehaviour
    {
        [SerializeField] private NewDebugManager debugManager;

        [ContextMenu("QA/DebugLog/Run")]
        public void Run()
        {
            ApplySettings();
            DumpCurrentState();
            EmitLevelSamples();
            TestDeduplication();
        }

        private void ApplySettings()
        {
            if (debugManager != null)
            {
                debugManager.ApplyConfiguration();
                return;
            }

            DebugUtility.LogWarning(typeof(DebugLogSettingsQATester),
                "[QA][DebugLog] NewDebugManager não definido; usando estado atual do DebugUtility.");
        }

        private void DumpCurrentState()
        {
            DebugUtility.Log(typeof(DebugLogSettingsQATester),
                $"[QA][DebugLog] Estado => Global={DebugUtility.IsGlobalDebugEnabled}, Verbose={DebugUtility.IsVerboseLoggingEnabled}, Fallbacks={DebugUtility.IsFallbacksEnabled}, RepeatedVerbose={DebugUtility.IsRepeatedCallVerboseEnabled}, DefaultLevel={DebugUtility.DefaultDebugLevel}.");
        }

        private void EmitLevelSamples()
        {
            DebugUtility.LogError(typeof(DebugLogSettingsQATester), "[QA][DebugLog] Sample Error");
            DebugUtility.LogWarning(typeof(DebugLogSettingsQATester), "[QA][DebugLog] Sample Warning");
            DebugUtility.Log(typeof(DebugLogSettingsQATester), "[QA][DebugLog] Sample Log");
            DebugUtility.LogVerbose(typeof(DebugLogSettingsQATester), "[QA][DebugLog] Sample Verbose");
        }

        private void TestDeduplication()
        {
            DebugUtility.Log(typeof(DebugLogSettingsQATester),
                "[QA][DebugLog] Teste de dedupe: duas chamadas verbose com deduplicate=true no mesmo frame (espera 1 log).");

            DebugUtility.LogVerbose(typeof(DebugLogSettingsQATester),
                "[QA][DebugLog] Dedupe frame check", deduplicate: true);
            DebugUtility.LogVerbose(typeof(DebugLogSettingsQATester),
                "[QA][DebugLog] Dedupe frame check", deduplicate: true);
        }
    }
}
