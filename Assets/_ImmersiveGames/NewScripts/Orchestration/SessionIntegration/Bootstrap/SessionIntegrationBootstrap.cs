using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Bootstrap;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Bootstrap
{
    public static class SessionIntegrationBootstrap
    {
        private static bool _installerPhaseComposed;
        private static bool _runtimeComposed;

        public static void ComposeInstallerPhase()
        {
            if (_installerPhaseComposed)
            {
                return;
            }

            _installerPhaseComposed = true;

            DebugUtility.Log(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] Installer phase no-op completed; runtime composition deferred to Navigation-backed bootstrap phase.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            EnsureGameplaySessionFlowContinuityService(bootstrapConfig);
            EnsureGameplaySessionRunResetService();
            SessionTransitionBootstrap.ComposeRuntime();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplaySessionFlowContinuityService(BootstrapConfigAsset bootstrapConfig)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowContinuityService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameNavigationService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IRestartContextService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            IPhaseResetExecutor phaseResetExecutor = new PhaseResetExecutor(restartContextService);
            IPhaseDefinitionCatalog phaseDefinitionCatalog = ResolveOptionalPhaseDefinitionCatalog(bootstrapConfig);

            var service = new GameplaySessionFlowContinuityService(
                navigationService,
                restartContextService,
                phaseResetExecutor,
                phaseDefinitionCatalog);

            DependencyManager.Provider.RegisterGlobal<IGameplaySessionFlowContinuityService>(service);

            DebugUtility.LogVerbose(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] IGameplaySessionFlowContinuityService registrado como continuity seam canonical.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplaySessionRunResetService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionRunResetService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IRestartContextService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameNavigationService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var phaseCatalogRuntimeStateService) || phaseCatalogRuntimeStateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IPhaseCatalogRuntimeStateService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            var service = new GameplaySessionRunResetService(
                restartContextService,
                navigationService,
                phaseCatalogRuntimeStateService);

            DependencyManager.Provider.RegisterGlobal<IGameplaySessionRunResetService>(service);

            DebugUtility.LogVerbose(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] IGameplaySessionRunResetService registrado como run-reset seam canonical.",
                DebugUtility.Colors.Info);
        }

        private static IPhaseDefinitionCatalog ResolveOptionalPhaseDefinitionCatalog(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig?.NavigationCatalog is not GameNavigationCatalogAsset navigationCatalog)
            {
                return null;
            }

            if (!navigationCatalog.IsGameplayPhaseEnabledOrFail())
            {
                return null;
            }

            return navigationCatalog.ResolveGameplayPhaseCatalogOrFail();
        }
    }
}
