using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.Gates.Interop;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Pause / Readiness gate
        // --------------------------------------------------------------------

        private static void RegisterPauseBridge(ISimulationGateService gateService)
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Pause] GamePauseGateBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (gateService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        "[Pause] ISimulationGateService indisponível; GamePauseGateBridge não pôde ser inicializado.");
                    return;
                }
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Pause] GamePauseGateBridge registrado (EventBus → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        private static void InitializeReadinessGate(ISimulationGateService gateService)
        {
            if (gateService == null)
            {
                // fallback: tenta resolver aqui (best-effort)
                if (!DependencyManager.Provider.TryGetGlobal(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        "[Readiness] ISimulationGateService indisponível. Scene Flow readiness ficará sem proteção de gate.");
                    return;
                }
            }

            if (DependencyManager.Provider.TryGetGlobal<GameReadinessService>(out var registered) && registered != null)
            {
                _gameReadinessService = registered;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Readiness] GameReadinessService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            _gameReadinessService = new GameReadinessService(gateService);
            DependencyManager.Provider.RegisterGlobal(_gameReadinessService);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate).",
                DebugUtility.Colors.Info);
        }

    }
}
