using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Bootstrap
{
    /// <summary>
    /// Installer do WorldReset.
    ///
    /// Responsabilidade:
    /// - registrar contratos, servicos e bridges do reset macro no boot;
    /// - nao compor runtime operacional nem depender de bootstrap posterior;
    /// - nao exigir gate de transicao como prerequisito obrigatorio.
    /// </summary>
    public static class WorldResetInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            RegisterWorldResetService();
            RegisterWorldResetCommands();
            RegisterSceneFlowWorldResetDriver();
            RegisterWorldResetRequestService();

            _installed = true;

            DebugUtility.Log(typeof(WorldResetInstaller),
                "[WorldReset] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterWorldResetService()
        {
            var service = ResolveOrCreateWorldResetService();

            RegisterIfMissing<IWorldResetService>(
                () => service,
                "[WorldReset] IWorldResetService ja registrado no DI global.",
                "[WorldReset] IWorldResetService registrado no DI global.");

            RegisterIfMissing(
                () => service,
                "[WorldReset] WorldResetService ja registrado no DI global.",
                "[WorldReset] WorldResetService registrado no DI global.");
        }

        private static void RegisterWorldResetCommands()
        {
            RegisterIfMissing<IWorldResetCommands>(
                () => new WorldResetCommands(),
                "[WorldReset] IWorldResetCommands ja registrado no DI global.",
                "[WorldReset] WorldResetCommands registrado no DI global.");
        }

        private static void RegisterSceneFlowWorldResetDriver()
        {
            RegisterIfMissing(
                () => new SceneFlowWorldResetDriver(),
                "[ResetInterop] SceneFlowWorldResetDriver ja registrado no DI global.",
                "[ResetInterop] SceneFlowWorldResetDriver registrado no DI global.");
        }

        private static void RegisterWorldResetRequestService()
        {
            RegisterIfMissing<IWorldResetRequestService>(
                () => new WorldResetRequestService(ResolveSimulationGateServiceOrNull()),
                "[WorldReset] IWorldResetRequestService ja registrado no DI global.",
                "[WorldReset] WorldResetRequestService registrado no DI global.");
        }

        private static WorldResetService ResolveOrCreateWorldResetService()
        {
            if (DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var existingConcrete) && existingConcrete != null)
            {
                return existingConcrete;
            }

            if (DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out var existingInterface) && existingInterface is WorldResetService existingService)
            {
                return existingService;
            }

            return new WorldResetService();
        }

        private static ISimulationGateService ResolveSimulationGateServiceOrNull()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) && gateService != null)
            {
                return gateService;
            }

            return null;
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(WorldResetInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(WorldResetInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
