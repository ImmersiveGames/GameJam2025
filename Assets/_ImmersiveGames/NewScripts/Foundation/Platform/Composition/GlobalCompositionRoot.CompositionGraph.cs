using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.Bootstrap;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.Bootstrap;
using _ImmersiveGames.NewScripts.InputModes.Bootstrap;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Installers;
using _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap;
using _ImmersiveGames.NewScripts.SceneFlow.Installers;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Bootstrap;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Installers;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
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

            CompositionProfileKind compositionProfile = ResolveCompositionProfileOrFail(bootstrapConfig);

            if (compositionProfile == CompositionProfileKind.Base11Sandbox)
            {
                DebugUtility.Log(typeof(GlobalCompositionRoot),
                    "[OBS][Composition][Profile] Base11Sandbox ativo: rails legados fora do profile minimo.",
                    DebugUtility.Colors.Info);
                steps.AddRange(GetBase11SandboxCompositionSteps());
            }
            else
            {
                bool phaseEnabled = ResolveGameplayPhaseEnablementOrFail(bootstrapConfig);
                steps.AddRange(GetLegacyCompositionSteps(phaseEnabled));
            }

            steps.Add(new CompositionPipelineStep(
                id: "SceneComposition",
                installer: _ => InstallSceneCompositionServices(),
                installerDependencies: System.Array.Empty<string>(),
                bootstrap: null,
                bootstrapDependencies: System.Array.Empty<string>()));

            return steps;
        }

        private static IReadOnlyList<CompositionPipelineStep> GetLegacyCompositionSteps(bool phaseEnabled)
        {
            // Ordem intencional:
            // - Installer: Audio antes de Preferences (Preferences depende do Audio instalado).
            // - Bootstrap: Preferences antes de Audio (Audio depende do snapshot de Preferences).
            var steps = new List<CompositionPipelineStep>(10)
            {
                CompositionPipelineStep.FromDescriptor(PreferencesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(AudioCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(GameplayCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(InputModesCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SceneFlowCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(NavigationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(SessionIntegrationCompositionDescriptor.Descriptor),
                CompositionPipelineStep.FromDescriptor(ActorsSystemCompositionDescriptor.Descriptor),
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

        private static IReadOnlyList<CompositionPipelineStep> GetBase11SandboxCompositionSteps()
        {
            return new List<CompositionPipelineStep>(3)
            {
                new CompositionPipelineStep(
                    id: "InputModes",
                    installer: bootstrapConfig => InputModesInstaller.Install(bootstrapConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: bootstrapConfig => InputModesRuntimeComposer.ComposeRuntime(bootstrapConfig),
                    bootstrapDependencies: System.Array.Empty<string>()),
                new CompositionPipelineStep(
                    id: "Base11SandboxOperationalRouting",
                    installer: bootstrapConfig => Base11SandboxOperationalRoutingComposer.Install(bootstrapConfig),
                    installerDependencies: new[] { "RuntimePolicy" },
                    bootstrap: bootstrapConfig => Base11SandboxOperationalRoutingComposer.ComposeRuntime(bootstrapConfig),
                    bootstrapDependencies: new[] { "InputModes" }),
            };
        }

        private static CompositionProfileKind ResolveCompositionProfileOrFail(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                throw new System.InvalidOperationException("[FATAL][Config][Composition] BootstrapConfigAsset obrigatorio ausente para resolver compositionProfile.");
            }

            RuntimeModeConfig runtimeModeConfig = bootstrapConfig.RuntimeModeConfig;
            if (runtimeModeConfig == null)
            {
                throw new System.InvalidOperationException($"[FATAL][Config][Composition] RuntimeModeConfig obrigatorio ausente no BootstrapConfigAsset. bootstrap='{bootstrapConfig.name}'.");
            }

            return runtimeModeConfig.compositionProfile;
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


