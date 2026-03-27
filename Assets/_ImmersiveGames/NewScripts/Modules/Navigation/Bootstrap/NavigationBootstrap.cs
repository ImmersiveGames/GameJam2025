using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bootstrap
{
    /// <summary>
    /// Runtime composer do Navigation.
    ///
    /// Responsabilidade:
    /// - compor e ativar o runtime do modulo depois que os installers relevantes concluirem;
    /// - nao registrar contratos de boot.
    /// </summary>
    public static class NavigationBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(NavigationBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            EnsureNavigationService(bootstrapConfig);
            EnsureExitToMenuCoordinator();
            EnsureMacroRestartCoordinator();
            EnsureLevelSelectedRestartSnapshotBridge();
            EnsureNavigationLevelRouteBgmBridge(bootstrapConfig);

            _runtimeComposed = true;

            DebugUtility.Log(typeof(NavigationBootstrap),
                "[Navigation] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureNavigationService(BootstrapConfigAsset bootstrapConfig)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existingService) && existingService != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] ISceneTransitionService ausente no DI global antes da composicao runtime.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] IGameNavigationCatalog ausente no DI global antes da composicao runtime.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Navigation] IRestartContextService ausente no DI global antes da composicao runtime.");
            }

            var service = new GameNavigationService(
                sceneFlow,
                catalog,
                restartContextService);

            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(NavigationBootstrap),
                $"[Navigation] GameNavigationService composto no runtime (Catalog={catalog.GetType().Name}, RestartContext={restartContextService.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureExitToMenuCoordinator()
        {
            RegisterIfMissing(
                () => new ExitToMenuCoordinator(),
                typeof(ExitToMenuCoordinator),
                "[Navigation] ExitToMenuCoordinator ja registrado no DI global.",
                "[Navigation] ExitToMenuCoordinator registrado no DI global.");
        }

        private static void EnsureMacroRestartCoordinator()
        {
            RegisterIfMissing(
                () => new MacroRestartCoordinator(),
                typeof(MacroRestartCoordinator),
                "[Navigation] MacroRestartCoordinator ja registrado no DI global.",
                "[Navigation] MacroRestartCoordinator registrado no DI global.");
        }

        private static void EnsureLevelSelectedRestartSnapshotBridge()
        {
            RegisterIfMissing(
                () => new LevelSelectedRestartSnapshotBridge(),
                typeof(LevelSelectedRestartSnapshotBridge),
                "[Navigation] LevelSelectedRestartSnapshotBridge ja registrado no DI global.",
                "[Navigation] LevelSelectedRestartSnapshotBridge registrado no DI global.");
        }

        private static void EnsureNavigationLevelRouteBgmBridge(BootstrapConfigAsset bootstrapConfig)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                DebugUtility.LogWarning(typeof(NavigationBootstrap),
                    "[Audio][BGM][Bridge] Skipped registration: IAudioBgmService unavailable.");
                return;
            }

            var navigationCatalog = bootstrapConfig.NavigationCatalog as GameNavigationCatalogAsset;
            if (navigationCatalog == null)
            {
                DebugUtility.LogWarning(typeof(NavigationBootstrap),
                    "[Audio][BGM][Bridge] Skipped registration: NavigationCatalog missing in bootstrap.");
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext);

            RegisterIfMissing(
                () => new NavigationLevelRouteBgmBridge(
                    bgmService,
                    navigationCatalog,
                    restartContext),
                typeof(NavigationLevelRouteBgmBridge),
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge already registered in global DI.",
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge registered in global DI.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, Type contextType, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(contextType, alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(contextType, registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
