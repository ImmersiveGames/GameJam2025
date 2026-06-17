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
                "Plan=StringsToDirectRefs v1",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "Plan=DataCleanup v1 (post StringsToDirectRefs v1)",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "DataCleanupV1Anchor snapshot='DataCleanup-v1.md'",
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

