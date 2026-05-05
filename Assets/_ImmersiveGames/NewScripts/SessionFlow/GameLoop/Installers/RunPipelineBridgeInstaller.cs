using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.RunResultStage.GameLoopRunOutcome;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Installer temporario para integracao do Run Pipeline.
    /// </summary>
    public static class RunPipelineBridgeInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterGameRunEndRequestService();
            RegisterGameRunOutcomeService();
            RegisterGameRunOutcomeRequestBridge();

            _installed = true;

            DebugUtility.Log(typeof(RunPipelineBridgeInstaller),
                "[OBS][RunPipeline][Integration] services_registered",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            RegisterIfMissing<IGameRunEndRequestService>(
                () => new GameRunEndRequestService(),
                "[RunPipeline][Integration] IGameRunEndRequestService ja registrado no DI global.",
                "[RunPipeline][Integration] GameRunEndRequestService registrado no DI global.");
        }

        private static void RegisterGameRunOutcomeService()
        {
            RegisterIfMissing<IGameRunOutcomeService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunPipeline] IGameRunPlayingStateGuard ausente ao registrar IGameRunOutcomeService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunPipeline] IGameLoopService ausente ao registrar IGameRunOutcomeService.");
                    }

                    return new GameRunOutcomeService(gameplayStateGuard, gameLoopService);
                },
                "[RunPipeline][Integration] IGameRunOutcomeService ja registrado no DI global.",
                "[RunPipeline][Integration] GameRunOutcomeService registrado no DI global.");
        }

        private static void RegisterGameRunOutcomeRequestBridge()
        {
            RegisterIfMissing<GameRunOutcomeRequestBridge>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunPipeline] IGameRunOutcomeService ausente no DI global antes de registrar o GameRunOutcomeRequestBridge.");
                    }

                    return new GameRunOutcomeRequestBridge(outcomeService);
                },
                "[RunPipeline][Integration] GameRunOutcomeRequestBridge ja registrado no DI global.",
                "[RunPipeline][Integration] GameRunOutcomeRequestBridge registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(RunPipelineBridgeInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(RunPipelineBridgeInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
