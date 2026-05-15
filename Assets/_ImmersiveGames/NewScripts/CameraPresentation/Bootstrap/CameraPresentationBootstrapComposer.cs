using System;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Bootstrap
{
    public static class CameraPresentationBootstrapComposer
    {
        private static bool _runtimeComposed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraPresentation] RuntimeModeConfig obrigatorio ausente no installer.");
            }

            DebugUtility.Log(typeof(CameraPresentationBootstrapComposer),
                "[OBS][CameraPresentation][Composer] installer concluded.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(CameraPresentationBootstrapComposer));

            if (_runtimeComposed)
            {
                DebugUtility.Log(typeof(CameraPresentationBootstrapComposer),
                    "[OBS][CameraPresentation][Composer] compose skipped reason='already_composed'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraPresentation] RuntimeModeConfig obrigatorio ausente no bootstrap.");
            }

            DependencyManager dependencyManager = DependencyManager.Instance;
            if (dependencyManager == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraPresentation] DependencyManager.Instance obrigatorio ausente no bootstrap.");
            }

            var registry = new DependencyManagerCameraPresentationRuntimeRegistry(dependencyManager);
            var composer = new CameraPresentationRuntimeComposer();

            if (!composer.TryCompose(
                    registry,
                    out CameraPresentationRuntimeCompositionResult result,
                    out string reason))
            {
                throw new InvalidOperationException(
                    $"[FATAL][CameraPresentation][Composer] compose failed reason='{reason}' resultReason='{result?.Reason}' directorRegistered='{result?.DirectorRegistered}' preparationExecutorRegistered='{result?.PreparationExecutorRegistered}'.");
            }

            _runtimeComposed = true;

            DebugUtility.Log(typeof(CameraPresentationBootstrapComposer),
                $"[OBS][CameraPresentation][Composer] director registered type='{typeof(CinemachineActivityCameraDirector).Name}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(CameraPresentationBootstrapComposer),
                $"[OBS][CameraPresentation][Composer] preparation executor registered type='{typeof(ActivityCameraPreparationExecutor).Name}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(CameraPresentationBootstrapComposer),
                $"[OBS][CameraPresentation][Composer] runtime composed reason='{result.Reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
