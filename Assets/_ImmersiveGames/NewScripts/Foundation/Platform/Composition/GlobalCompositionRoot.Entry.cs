/*
 * ChangeLog
 * - Entrada de infraestrutura mínima (Scene/DI) para NewScripts.
 *
 * Ajustes (jan/2026):
 * - Reduzidas resoluções repetidas no DI global (evita warnings de "chamada repetida" no frame 0).
 * - Removido registro duplicado de coordinators antigos de reset/scene flow (centralizado no wiring atual).
 *
 * Nota (QA):
 * - O coordinator deve resolver dependências no momento do sync para que overrides de QA no DI sejam observados.
 *
 * Reorganização (jan/2026):
 * - Arquivo reordenado por seções (Init -> Pipeline -> Registradores -> Helpers), sem mudar assinaturas.
 */

using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    /// <summary>
    /// Entry point for the NewScripts project area.
    /// Commit 1: minimal global infrastructure (no gameplay, no spawn, no scene transitions).
    /// </summary>
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // State / Constants
        // --------------------------------------------------------------------

        private static bool _initialized;
        // --------------------------------------------------------------------
        // Entry
        // --------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if !NEWSCRIPTS_MODE
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                "NEWSCRIPTS_MODE desativado: GlobalCompositionRoot ignorado.");
            return;
#else
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Config] Plan=StringsToDirectRefs v1",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Config] Plan=DataCleanup v1 (post StringsToDirectRefs v1)",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Config] DataCleanupV1Anchor snapshot='SceneFlow-Config-Snapshot-DataCleanup-v1.md'",
                DebugUtility.Colors.Info);
            RegisterEssentialServicesOnly();

            DebugUtility.Log(
                typeof(GlobalCompositionRoot),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
#endif
        }

        private static void InitializeLogging()
        {
            DebugUtility.ApplyEarlyDefaultPolicy();
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                "[BOOT][Logging] EarlyDefault policy applied.",
                DebugUtility.Colors.Info);

            if (TryGetBootstrapConfigForLogging(out var bootstrapConfig, out string bootstrapVia, out string bootstrapReason))
            {
                LoggingConfigAsset loggingConfig = bootstrapConfig.LoggingConfig;
                if (loggingConfig != null)
                {
                    string source = $"BootstrapConfigAsset/{bootstrapVia}";
                    DebugUtility.ApplyLoggingPolicyFromAsset(loggingConfig, source);
                    DebugUtility.Log(typeof(GlobalCompositionRoot),
                        $"[STARTUP][Logging] Final policy applied from LoggingConfigAsset. source='{source}' asset='{loggingConfig.name}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                ApplyHardcodedFallbackLoggingPolicy(
                    $"bootstrap_without_logging_config via='{bootstrapVia}' bootstrap='{bootstrapConfig.name}'");
                return;
            }

            ApplyHardcodedFallbackLoggingPolicy($"bootstrap_unresolved reason='{bootstrapReason}'");
        }

        private static void ApplyHardcodedFallbackLoggingPolicy(string reason)
        {
            DebugUtility.ApplyLoggingPolicyFromBootstrap(
                defaultLevel: DebugLevel.Verbose,
                verboseEnabled: Application.isEditor,
                fallbacksEnabled: Application.isEditor,
                globalDebugEnabled: true,
                repeatedVerboseEnabled: true,
                source: "FallbackHardcoded");

            DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                $"[STARTUP][Logging] Applied hardcoded fallback logging policy. reason='{reason}'.");
        }

        private static void EnsureDependencyProvider()
        {
            if (DependencyManager.HasInstance)
            {
                return;
            }

            _ = DependencyManager.Provider;
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), "DependencyManager created for global scope.");
        }

    }
}

