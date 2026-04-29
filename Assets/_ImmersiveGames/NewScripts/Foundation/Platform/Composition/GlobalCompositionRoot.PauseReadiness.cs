using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Readiness gate
        // --------------------------------------------------------------------

        private static void InitializeReadinessGate(ISimulationGateService gateService)
        {
            if (gateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GlobalCompositionRoot] ISimulationGateService obrigatorio ausente para inicializar readiness.");
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

