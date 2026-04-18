using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
namespace _ImmersiveGames.NewScripts.InputModes.Bootstrap
{
    public static class InputModesRuntimeComposer
    {
        private const string CanonicalTrail =
            "InputModeRequestEvent->InputModeCoordinator->IInputModeService->InputModeChangedEvent";

        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(InputModesRuntimeComposer));

            if (_runtimeComposed)
            {
                return;
            }

            _ = bootstrapConfig;

            EnsureCanonicalTrailOrFail(requireCoordinator: false);
            EnsureCoordinatorOrFail();
            EnsureCanonicalTrailOrFail(requireCoordinator: true);

            _runtimeComposed = true;

            DebugUtility.Log(typeof(InputModesRuntimeComposer),
                $"[OBS][InputModes][Pipeline] status='ready' canonicalTrail='{CanonicalTrail}'.",
                DebugUtility.Colors.Success);
        }

        private static void EnsureCoordinatorOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<InputModeCoordinator>(out var existingCoordinator) && existingCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(InputModesRuntimeComposer),
                    "[OBS][InputModes][Pipeline] coordinator='already_registered'.",
                    DebugUtility.Colors.Info);
                return;
            }

            var coordinator = new InputModeCoordinator();
            DependencyManager.Provider.RegisterGlobal(coordinator);

            DebugUtility.Log(typeof(InputModesRuntimeComposer),
                "[OBS][InputModes][Pipeline] coordinator='registered'.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureCanonicalTrailOrFail(bool requireCoordinator)
        {
            if (DependencyManager.Provider == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModes] DependencyManager.Provider indisponivel no bootstrap.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service) || service == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][InputModes] Missing IInputModeService for canonical trail '{CanonicalTrail}'.");
            }

            if (service is not InputModeService)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][InputModes] IInputModeService incompatvel. expected='{nameof(InputModeService)}' actual='{service.GetType().Name}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IInputModeStateService>(out var stateService) || stateService == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][InputModes] Missing IInputModeStateService for canonical trail '{CanonicalTrail}'.");
            }

            if (!requireCoordinator)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<InputModeCoordinator>(out var coordinator) || coordinator == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][InputModes] Missing InputModeCoordinator for canonical trail '{CanonicalTrail}'.");
            }
        }
    }
}
