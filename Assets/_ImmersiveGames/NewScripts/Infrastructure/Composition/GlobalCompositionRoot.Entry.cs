/*
 * ChangeLog
 * - Registrado IContentSwapContextService (ContentSwapContextService) no DI global (ADR-0016).
 * - ContentSwap permanece InPlace-only (sem integração com SceneFlow).
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar física.
 * - StateDependentService agora usa apenas StateDependentService (legacy removido).
 * - Entrada de infraestrutura mínima (Gate/WorldLifecycle/DI/Câmera/StateBridge) para NewScripts.
 * - (Opção B) Registrado GameLoopSceneFlowCoordinator para coordenar Start via SceneFlow
 *   (GameStartRequestedEvent -> Transition -> ScenesReady -> RequestStart/Ready).
 *
 * Ajustes (jan/2026):
 * - Reduzidas resoluções repetidas no DI global (evita warnings de "chamada repetida" no frame 0):
 *   - Resolve IGameLoopService uma vez e injeta nos registradores de GameRunStatus/Outcome.
 *   - Resolve ISimulationGateService uma vez e injeta em GameReadinessService e PauseBridge.
 * - Removido registro duplicado de WorldLifecycleRuntimeCoordinator (centralizado em RegisterSceneFlowNative()).
 *
 * Nota (QA):
 * - O coordinator NÃO deve cachear IGameLoopService; deve resolver no momento do sync
 *   para que overrides de QA no DI sejam observados.
 *
 * Reorganização (jan/2026):
 * - Arquivo reordenado por seções (Init -> Pipeline -> Registradores -> Helpers), sem mudar assinaturas.
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

        // Opção B: mantém referência viva do coordinator (evita GC / descarte prematuro).
        private static GameLoopSceneFlowCoordinator _sceneFlowCoordinator;

        // Profile fixo do start (para filtrar ScenesReady/ResetCompleted).
        private static readonly SceneFlowProfileId StartProfileId = SceneFlowProfileId.Startup;

        // Scene names (Unity: SceneManager.GetActiveScene().name)
        private const string SceneNewBootstrap = "NewBootstrap";
        private const string SceneMenu = "MenuScene";
        private const string SceneUIGlobal = "UIGlobalScene";

        // --------------------------------------------------------------------
        // Entry
        // --------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
            RegisterEssentialServicesOnly();

            DebugUtility.Log(
                typeof(GlobalCompositionRoot),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
#endif
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
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
