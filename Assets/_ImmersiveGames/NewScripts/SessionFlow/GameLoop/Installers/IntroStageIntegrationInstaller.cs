using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterExecution;
using _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterResolution;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ContentContract;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ExecuteSkipPolicy;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Installer temporario de integracao do IntroStage.
    /// </summary>
    public static class IntroStageIntegrationInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterIntroStagePresenterScopeResolver();
            RegisterIntroStageSessionService();
            RegisterIntroStagePresenterRegistry();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterIntroStageLifecycleStateService();
            RegisterIntroStageLifecycleDispatchService();
            RegisterGameplaySceneClassifier();
            RegisterIntroStageLifecycleOrchestrator();

            _installed = true;

            DebugUtility.Log(typeof(IntroStageIntegrationInstaller),
                "[OBS][IntroStage][Integration] Installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStagePresenterScopeResolver()
        {
            RegisterIfMissing<IIntroStagePresenterScopeResolver>(
                () => new IntroStagePresenterScopeResolver(),
                "[IntroStage][Integration] IIntroStagePresenterScopeResolver ja registrado no DI global.",
                "[IntroStage][Integration] IntroStagePresenterScopeResolver registrado no DI global.");
        }

        private static void RegisterIntroStageSessionService()
        {
            RegisterIfMissing<IIntroStageSessionService>(
                () => new IntroStageSessionService(),
                "[IntroStage][Integration] IIntroStageSessionService ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageSessionService registrado no DI global.");
        }

        private static void RegisterIntroStagePresenterRegistry()
        {
            RegisterIfMissing<IIntroStagePresenterRegistry>(
                () => new IntroStagePresenterHost(),
                "[IntroStage][Integration] IIntroStagePresenterRegistry ja registrado no DI global.",
                "[IntroStage][Integration] IntroStagePresenterHost registrado no DI global.");
        }

        private static void RegisterIntroStageCoordinator()
        {
            RegisterIfMissing<IIntroStageCoordinator>(
                () => new IntroStageCoordinator(),
                "[IntroStage][Integration] IIntroStageCoordinator ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageCoordinator registrado no DI global.");
        }

        private static void RegisterIntroStageControlService()
        {
            RegisterIfMissing<IIntroStageControlService>(
                () => new IntroStageControlService(),
                "[IntroStage][Integration] IIntroStageControlService ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageControlService registrado no DI global.");
        }

        private static void RegisterGameplaySceneClassifier()
        {
            RegisterIfMissing<IGameplaySceneClassifier>(
                () => new DefaultGameplaySceneClassifier(),
                "[IntroStage][Integration] IGameplaySceneClassifier ja registrado no DI global.",
                "[IntroStage][Integration] DefaultGameplaySceneClassifier registrado no DI global.");
        }

        private static void RegisterIntroStageLifecycleOrchestrator()
        {
            RegisterIfMissing<IntroStageLifecycleOrchestrator>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageLifecycleStateService>(out var stateService) || stateService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][IntroStage][Integration] IIntroStageLifecycleStateService ausente ao registrar IntroStageLifecycleOrchestrator.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageLifecycleDispatchService>(out var dispatchService) || dispatchService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][IntroStage][Integration] IIntroStageLifecycleDispatchService ausente ao registrar IntroStageLifecycleOrchestrator.");
                    }

                    return new IntroStageLifecycleOrchestrator(stateService, dispatchService);
                },
                "[IntroStage][Integration] IntroStageLifecycleOrchestrator ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageLifecycleOrchestrator registrado no DI global.");
        }

        private static void RegisterIntroStageLifecycleStateService()
        {
            RegisterIfMissing<IIntroStageLifecycleStateService>(
                () => new IntroStageLifecycleStateService(),
                "[IntroStage][Integration] IIntroStageLifecycleStateService ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageLifecycleStateService registrado no DI global.");
        }

        private static void RegisterIntroStageLifecycleDispatchService()
        {
            RegisterIfMissing<IIntroStageLifecycleDispatchService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator) || coordinator == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][IntroStage][Integration] IIntroStageCoordinator ausente ao registrar IntroStageLifecycleDispatchService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStagePresenterRegistry>(out var presenterRegistry) || presenterRegistry == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][IntroStage][Integration] IIntroStagePresenterRegistry ausente ao registrar IntroStageLifecycleDispatchService.");
                    }

                    return new IntroStageLifecycleDispatchService(coordinator, presenterRegistry);
                },
                "[IntroStage][Integration] IIntroStageLifecycleDispatchService ja registrado no DI global.",
                "[IntroStage][Integration] IntroStageLifecycleDispatchService registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(IntroStageIntegrationInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(IntroStageIntegrationInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
