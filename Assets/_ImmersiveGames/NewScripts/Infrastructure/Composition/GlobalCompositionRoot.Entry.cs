/*
 * ChangeLog
 * - Registrado IContentSwapContextService (ContentSwapContextService) no DI global (ADR-0016).
 * - ContentSwap permanece InPlace-only (sem integraÃ§Ã£o com SceneFlow).
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar fÃ­sica.
 * - StateDependentService agora usa apenas StateDependentService (legacy removido).
 * - Entrada de infraestrutura mÃ­nima (Gate/WorldLifecycle/DI/CÃ¢mera/StateBridge) para NewScripts.
 * - (OpÃ§Ã£o B) Registrado GameLoopSceneFlowCoordinator para coordenar Start via SceneFlow
 *   (GameStartRequestedEvent -> Transition -> ScenesReady -> RequestStart/Ready).
 *
 * Ajustes (jan/2026):
 * - Reduzidas resoluÃ§Ãµes repetidas no DI global (evita warnings de "chamada repetida" no frame 0):
 *   - Resolve IGameLoopService uma vez e injeta nos registradores de GameRunStatus/Outcome.
 *   - Resolve ISimulationGateService uma vez e injeta em GameReadinessService e PauseBridge.
 * - Removido registro duplicado de WorldLifecycleRuntimeCoordinator (centralizado em RegisterSceneFlowNative()).
 *
 * Nota (QA):
 * - O coordinator NÃƒO deve cachear IGameLoopService; deve resolver no momento do sync
 *   para que overrides de QA no DI sejam observados.
 *
 * ReorganizaÃ§Ã£o (jan/2026):
 * - Arquivo reordenado por seÃ§Ãµes (Init -> Pipeline -> Registradores -> Helpers), sem mudar assinaturas.
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
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
        private static GameReadinessService _gameReadinessService;

        // OpÃ§Ã£o B: mantÃ©m referÃªncia viva do coordinator (evita GC / descarte prematuro).
        private static GameLoopSceneFlowCoordinator _sceneFlowCoordinator;

        // Scene names (Unity: SceneManager.GetActiveScene().name)
        private const string SceneNewBootstrap = "NewBootstrap";
        private const string SceneMenu = "MenuScene";
        private const string SceneUIGlobal = "UIGlobalScene";

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
                "âœ… NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
#endif
        }

        private static void InitializeLogging()
        {
            bool verboseEnabled = Application.isEditor;
            bool fallbacksEnabled = Application.isEditor;

            DebugUtility.ApplyLoggingPolicyFromBootstrap(
                defaultLevel: DebugLevel.Verbose,
                verboseEnabled: verboseEnabled,
                fallbacksEnabled: fallbacksEnabled,
                globalDebugEnabled: true,
                repeatedVerboseEnabled: true);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), "NewScripts logging configured.");
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


