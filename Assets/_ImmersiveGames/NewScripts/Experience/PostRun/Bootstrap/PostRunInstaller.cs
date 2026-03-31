using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Presentation;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Bootstrap
{
    /// <summary>
    /// Installer do PostRun.
    ///
    /// Responsabilidade:
    /// - registrar contratos e serviços de post-game no boot;
    /// - nao compor runtime nem criar bridges.
    /// </summary>
    public static class PostRunInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterPostRunResultService();
            RegisterPostRunOwnershipService();
            RegisterPostStageControlService();
            RegisterPostStagePresenterScopeResolver();
            RegisterPostStagePresenterRegistry();
            RegisterPostStageCoordinator();
            RegisterPostRunHandoffService();

            _installed = true;

            DebugUtility.Log(typeof(PostRunInstaller),
                "[PostRun] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostRunResultService()
        {
            RegisterIfMissing<IPostRunResultService>(
                () => new PostRunResultService(),
                "[PostRun] IPostRunResultService ja registrado no DI global.",
                "[PostRun] PostRunResultService registrado no DI global.");
        }

        private static void RegisterPostRunOwnershipService()
        {
            RegisterIfMissing<IPostRunOwnershipService>(
                () => new PostRunOwnershipService(),
                "[PostRun] IPostRunOwnershipService ja registrado no DI global.",
                "[PostRun] PostRunOwnershipService registrado no DI global.");
        }

        private static void RegisterPostStageControlService()
        {
            RegisterIfMissing<IPostStageControlService>(
                () => new PostStageControlService(),
                "[PostRun] IPostStageControlService ja registrado no DI global.",
                "[PostRun] PostStageControlService registrado no DI global.");
        }

        private static void RegisterPostStagePresenterScopeResolver()
        {
            RegisterIfMissing<IPostStagePresenterScopeResolver>(
                () => new PostStagePresenterScopeResolver(),
                "[PostRun] IPostStagePresenterScopeResolver ja registrado no DI global.",
                "[PostRun] PostStagePresenterScopeResolver registrado no DI global.");
        }

        private static void RegisterPostStagePresenterRegistry()
        {
            RegisterIfMissing<IPostStagePresenterRegistry>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPostStageControlService>(out var controlService) || controlService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostStageControlService ausente ao registrar IPostStagePresenterRegistry.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostStagePresenterScopeResolver>(out var scopeResolver) || scopeResolver == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostStagePresenterScopeResolver ausente ao registrar IPostStagePresenterRegistry.");
                    }

                    return new PostStagePresenterRegistry(controlService, scopeResolver);
                },
                "[PostRun] IPostStagePresenterRegistry ja registrado no DI global.",
                "[PostRun] PostStagePresenterRegistry registrado no DI global.");
        }

        private static void RegisterPostStageCoordinator()
        {
            RegisterIfMissing<IPostStageCoordinator>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPostStageControlService>(out var controlService) || controlService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostStageControlService ausente ao registrar IPostStageCoordinator.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostStagePresenterRegistry>(out var presenterRegistry) || presenterRegistry == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostStagePresenterRegistry ausente ao registrar IPostStageCoordinator.");
                    }

                    return new PostStageCoordinator(controlService, presenterRegistry);
                },
                "[PostRun] IPostStageCoordinator ja registrado no DI global.",
                "[PostRun] PostStageCoordinator registrado no DI global.");
        }

        private static void RegisterPostRunHandoffService()
        {
            RegisterIfMissing<IPostRunHandoffService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPostStageCoordinator>(out var postStageCoordinator) || postStageCoordinator == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostStageCoordinator ausente ao registrar IPostRunHandoffService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostRunResultService>(out var resultService) || resultService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostRunResultService ausente ao registrar IPostRunHandoffService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostRunOwnershipService>(out var ownershipService) || ownershipService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IPostRunOwnershipService ausente ao registrar IPostRunHandoffService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostRun] IGameLoopService ausente ao registrar IPostRunHandoffService.");
                    }

                    return new PostRunHandoffService(
                        postStageCoordinator,
                        resultService,
                        ownershipService,
                        gameLoopService);
                },
                "[PostRun] IPostRunHandoffService ja registrado no DI global.",
                "[PostRun] PostRunHandoffService registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(PostRunInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(PostRunInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}

