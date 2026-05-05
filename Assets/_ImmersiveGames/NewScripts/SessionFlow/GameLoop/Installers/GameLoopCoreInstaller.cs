using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.RunResultStage.GameLoopRunOutcome;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Installer do core tecnico do GameLoop.
    /// Responsabilidade temporaria: apenas loop state e contratos diretos do executor.
    /// </summary>
    public static class GameLoopCoreInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterGameLoopService();
            RegisterPauseStateService();
            RegisterGameRunPlayingStateGuard();

            _installed = true;

            DebugUtility.Log(typeof(GameLoopCoreInstaller),
                "[OBS][GameLoop][Core] Core installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameLoopService()
        {
            RegisterIfMissing<IGameLoopService>(
                () => new GameLoopService(),
                "[GameLoop][Core] IGameLoopService ja registrado no DI global.",
                "[GameLoop][Core] GameLoopService registrado no DI global.");
        }

        private static void RegisterPauseStateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPauseStateService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopCoreInstaller),
                    "[OBS][GameLoop][Core] IPauseStateService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop][Core] IGameLoopService ausente ao registrar IPauseStateService.");
            }

            if (gameLoopService is not IPauseStateService pauseStateService)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop][Core] IGameLoopService nao implementa IPauseStateService.");
            }

            DependencyManager.Provider.RegisterGlobal(pauseStateService);
            DebugUtility.LogVerbose(typeof(GameLoopCoreInstaller),
                "[OBS][GameLoop][Core] IPauseStateService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunPlayingStateGuard()
        {
            RegisterIfMissing<IGameRunPlayingStateGuard>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop][Core] IGameLoopService ausente ao registrar IGameRunPlayingStateGuard.");
                    }

                    return new GameRunPlayingStateGuard(gameLoopService);
                },
                "[GameLoop][Core] IGameRunPlayingStateGuard ja registrado no DI global.",
                "[GameLoop][Core] GameRunPlayingStateGuard registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopCoreInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GameLoopCoreInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
