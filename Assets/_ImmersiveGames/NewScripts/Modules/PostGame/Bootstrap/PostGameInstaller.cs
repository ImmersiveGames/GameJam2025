using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.PostGame.Bootstrap
{
    /// <summary>
    /// Installer do PostGame.
    ///
    /// Responsabilidade:
    /// - registrar contratos e serviços de post-game no boot;
    /// - não compor runtime nem criar bridges.
    /// </summary>
    public static class PostGameInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterPostGameResultService();
            RegisterPostGameOwnershipService();
            RegisterPostStageControlService();
            RegisterPostStagePresenterScopeResolver();
            RegisterPostStagePresenterRegistry();
            RegisterPostStageCoordinator();

            _installed = true;

            DebugUtility.Log(typeof(PostGameInstaller),
                "[PostGame] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostGameResultService()
        {
            RegisterIfMissing<IPostGameResultService>(
                () => new PostGameResultService(),
                "[PostGame] IPostGameResultService ja registrado no DI global.",
                "[PostGame] PostGameResultService registrado no DI global.");
        }

        private static void RegisterPostGameOwnershipService()
        {
            RegisterIfMissing<IPostGameOwnershipService>(
                () => new PostGameOwnershipService(),
                "[PostGame] IPostGameOwnershipService ja registrado no DI global.",
                "[PostGame] PostGameOwnershipService registrado no DI global.");
        }

        private static void RegisterPostStageControlService()
        {
            RegisterIfMissing<IPostStageControlService>(
                () => new PostStageControlService(),
                "[PostGame] IPostStageControlService ja registrado no DI global.",
                "[PostGame] PostStageControlService registrado no DI global.");
        }

        private static void RegisterPostStagePresenterScopeResolver()
        {
            RegisterIfMissing<IPostStagePresenterScopeResolver>(
                () => new PostStagePresenterScopeResolver(),
                "[PostGame] IPostStagePresenterScopeResolver ja registrado no DI global.",
                "[PostGame] PostStagePresenterScopeResolver registrado no DI global.");
        }

        private static void RegisterPostStagePresenterRegistry()
        {
            RegisterIfMissing<IPostStagePresenterRegistry>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPostStageControlService>(out var controlService) || controlService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostGame] IPostStageControlService ausente ao registrar IPostStagePresenterRegistry.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostStagePresenterScopeResolver>(out var scopeResolver) || scopeResolver == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostGame] IPostStagePresenterScopeResolver ausente ao registrar IPostStagePresenterRegistry.");
                    }

                    return new PostStagePresenterRegistry(controlService, scopeResolver);
                },
                "[PostGame] IPostStagePresenterRegistry ja registrado no DI global.",
                "[PostGame] PostStagePresenterRegistry registrado no DI global.");
        }

        private static void RegisterPostStageCoordinator()
        {
            RegisterIfMissing<IPostStageCoordinator>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPostStageControlService>(out var controlService) || controlService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostGame] IPostStageControlService ausente ao registrar IPostStageCoordinator.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPostStagePresenterRegistry>(out var presenterRegistry) || presenterRegistry == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][PostGame] IPostStagePresenterRegistry ausente ao registrar IPostStageCoordinator.");
                    }

                    return new PostStageCoordinator(controlService, presenterRegistry);
                },
                "[PostGame] IPostStageCoordinator ja registrado no DI global.",
                "[PostGame] PostStageCoordinator registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(PostGameInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(PostGameInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
