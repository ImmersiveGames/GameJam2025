using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.ResetFlow.Interop.Runtime;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Guards;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Validation;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Installers
{
    /// <summary>
    /// Installer do WorldReset com composição explícita do eixo.
    /// Fail-fast para contratos estruturais obrigatórios.
    /// </summary>
    public static class WorldResetInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            _ = bootstrapConfig;

            if (_installed)
            {
                return;
            }

            RegisterLocalExecutorRegistry();
            RegisterPhaseResetOperationalHandoff();
            RegisterWorldResetService();
            RegisterWorldResetCommands();
            RegisterSceneFlowWorldResetDriver();
            RegisterWorldResetRequestService();
            EnsureWorldResetModuleComposition();

            _installed = true;

            DebugUtility.Log(typeof(WorldResetInstaller),
                "[WorldReset] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterLocalExecutorRegistry()
        {
            RegisterIfMissing<IWorldResetLocalExecutorRegistry>(
                () => new WorldResetLocalExecutorRegistry(),
                "[WorldReset] IWorldResetLocalExecutorRegistry ja registrado no DI global.",
                "[WorldReset] IWorldResetLocalExecutorRegistry registrado no DI global.");
        }

        private static void RegisterWorldResetService()
        {
            WorldResetService service = BuildWorldResetService();

            RegisterIfMissing<IWorldResetService>(
                () => service,
                "[WorldReset] IWorldResetService ja registrado no DI global.",
                "[WorldReset] IWorldResetService registrado no DI global.");

            RegisterIfMissing(
                () => service,
                "[WorldReset] WorldResetService ja registrado no DI global.",
                "[WorldReset] WorldResetService registrado no DI global.");
        }

        private static void RegisterPhaseResetOperationalHandoff()
        {
            RegisterIfMissing<IPhaseResetOperationalHandoffService>(
                () => new PhaseResetOperationalHandoffService(
                    ResolveRequired<IWorldResetLocalExecutorRegistry>("IWorldResetLocalExecutorRegistry")),
                "[WorldReset] IPhaseResetOperationalHandoffService ja registrado no DI global.",
                "[WorldReset] IPhaseResetOperationalHandoffService registrado no DI global.");
        }

        private static void RegisterWorldResetCommands()
        {
            RegisterIfMissing<IWorldResetCommands>(
                () => new WorldResetCommands(ResolveRequired<IWorldResetService>("IWorldResetService")),
                "[WorldReset] IWorldResetCommands ja registrado no DI global.",
                "[WorldReset] IWorldResetCommands registrado no DI global.");
        }

        private static void RegisterSceneFlowWorldResetDriver()
        {
            RegisterIfMissing(
                () => new SceneFlowWorldResetDriver(ResolveRequired<IWorldResetService>("IWorldResetService")),
                "[ResetInterop] SceneFlowWorldResetDriver ja registrado no DI global.",
                "[ResetInterop] SceneFlowWorldResetDriver registrado no DI global.");
        }

        private static void RegisterWorldResetRequestService()
        {
            RegisterIfMissing<IWorldResetRequestService>(
                () => new WorldResetRequestService(
                    ResolveRequired<IWorldResetService>("IWorldResetService"),
                    ResolveRequired<ISimulationGateService>("ISimulationGateService")),
                "[WorldReset] IWorldResetRequestService ja registrado no DI global.",
                "[WorldReset] IWorldResetRequestService registrado no DI global.");
        }

        private static void EnsureWorldResetModuleComposition()
        {
            ResolveRequired<IWorldResetService>("IWorldResetService");
            ResolveRequired<WorldResetService>("WorldResetService");
            ResolveRequired<IWorldResetCommands>("IWorldResetCommands");
            ResolveRequired<IWorldResetRequestService>("IWorldResetRequestService");
            ResolveRequired<SceneFlowWorldResetDriver>("SceneFlowWorldResetDriver");
            ResolveRequired<IWorldResetLocalExecutorRegistry>("IWorldResetLocalExecutorRegistry");
            ResolveRequired<IPhaseResetOperationalHandoffService>("IPhaseResetOperationalHandoffService");

            DebugUtility.Log(typeof(WorldResetInstaller),
                "[OBS][WorldReset] Runtime composition consolidada. scope='reset lifecycle macro + local executor registry + SceneFlow handoff'.",
                DebugUtility.Colors.Info);
        }

        private static WorldResetService BuildWorldResetService()
        {
            if (DependencyManager.Provider.TryGetGlobal<WorldResetService>(out WorldResetService existingConcrete) && existingConcrete != null)
            {
                return existingConcrete;
            }

            if (DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out IWorldResetService existingInterface) && existingInterface is WorldResetService existingService)
            {
                return existingService;
            }

            IDependencyProvider provider = ResolveRequired<IDependencyProvider>("IDependencyProvider");
            IWorldResetPolicy policy = ResolveRequired<IWorldResetPolicy>("IWorldResetPolicy");
            ISimulationGateService gateService = ResolveRequired<ISimulationGateService>("ISimulationGateService");
            IWorldResetLocalExecutorRegistry localExecutorRegistry = ResolveRequired<IWorldResetLocalExecutorRegistry>("IWorldResetLocalExecutorRegistry");

            var guards = new List<IWorldResetGuard>(1)
            {
                new SimulationGateWorldResetGuard(gateService)
            };

            var validators = new List<IWorldResetValidator>(1)
            {
                new WorldResetSignatureValidator()
            };

            WorldResetLifecyclePublisher lifecyclePublisher = new WorldResetLifecyclePublisher();
            WorldResetValidationPipeline validationPipeline = new WorldResetValidationPipeline(validators);
            WorldResetExecutor executor = new WorldResetExecutor(localExecutorRegistry);
            WorldResetPostResetValidator postResetValidator = new WorldResetPostResetValidator(provider);
            WorldResetOrchestrator orchestrator = new WorldResetOrchestrator(
                policy,
                guards,
                validationPipeline,
                executor,
                postResetValidator,
                lifecyclePublisher);

            return new WorldResetService(orchestrator, lifecyclePublisher);
        }

        private static T ResolveRequired<T>(string serviceName)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out T service) && service != null)
            {
                return service;
            }

            if (typeof(T) == typeof(IDependencyProvider))
            {
                return DependencyManager.Provider as T
                    ?? throw new InvalidOperationException("[FATAL][Config][WorldReset] DependencyManager.Provider obrigatorio ausente para compor o WorldReset runtime.");
            }

            throw new InvalidOperationException($"[FATAL][Config][WorldReset] {serviceName} obrigatorio ausente para compor o WorldReset runtime.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out T existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(WorldResetInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            T instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(WorldResetInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
