using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Bootstrap;
using ImmersiveGames.GameJam2025.Experience.PostRun.Bootstrap;
using ImmersiveGames.GameJam2025.Experience.Preferences.Bootstrap;
using ImmersiveGames.GameJam2025.Experience.Save.Bootstrap;
using ImmersiveGames.GameJam2025.Game.Gameplay.Bootstrap;
using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.Bootstrap;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Bootstrap;
using ImmersiveGames.GameJam2025.Orchestration.Navigation.Bootstrap;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Bootstrap;
using ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Bootstrap;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Bootstrap;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static IReadOnlyList<CompositionPipelineStep> GetCompositionPipelineSteps(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = new List<CompositionPipelineStep>(16);

            steps.Add(new CompositionPipelineStep(
                id: "RuntimePolicy",
                installer: _ => RegisterRuntimePolicyServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Pooling",
                installer: _ => InstallPoolingServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            steps.Add(new CompositionPipelineStep(
                id: "Gates",
                installer: _ => InstallGatesServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            bool phaseEnabled = ResolveGameplayPhaseEnablementOrFail(bootstrapConfig);

            steps.AddRange(GetModuleCompositionSteps(phaseEnabled));

            steps.Add(new CompositionPipelineStep(
                id: "SceneComposition",
                installer: _ => InstallSceneCompositionServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetModuleCompositionSteps(bool phaseEnabled)
        {
            // Ordem intencional:
            // - Installer: Audio antes de Preferences (Preferences depende do Audio instalado).
            // - Bootstrap: Preferences antes de Audio (Audio depende do snapshot de Preferences).
            var steps = new List<CompositionPipelineStep>(10)
            {
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(GameplayCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(GameLoopCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SceneFlowCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(NavigationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SessionIntegrationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(WorldResetCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SaveCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(RunEndRailCompositionDescriptor.Descriptor),
            };

            if (phaseEnabled)
            {
                steps.Insert(2, CompositionPipelineStep.FromDescriptor(PhaseDefinitionCompositionDescriptor.Descriptor));
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "[OBS][Composition][GameplaySessionFlow] Phase rail enabled at seam='GameplaySessionFlow/PhaseDefinition'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "[OBS][Composition][GameplaySessionFlow] Phase rail skipped because route/context is phase-disabled. seam='GameplaySessionFlow/PhaseDefinition'.",
                    DebugUtility.Colors.Info);
            }

            return steps;
        }

        private static bool ResolveGameplayPhaseEnablementOrFail(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                throw new System.InvalidOperationException("[FATAL][Config][Composition] BootstrapConfigAsset obrigatorio ausente para resolver phase-enabled/phase-disabled.");
            }

            if (bootstrapConfig.NavigationCatalog == null)
            {
                throw new System.InvalidOperationException("[FATAL][Config][Composition] GameNavigationCatalog obrigatorio ausente para resolver phase-enabled/phase-disabled.");
            }

            bool phaseEnabled = bootstrapConfig.NavigationCatalog.IsGameplayPhaseEnabledOrFail();
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Composition][GameplaySessionFlow] route-driven phase enablement resolved phaseEnabled={phaseEnabled}.",
                DebugUtility.Colors.Info);
            return phaseEnabled;
        }
    }
}


