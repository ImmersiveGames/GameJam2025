using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Debug
{
    public sealed class CameraPresentationComposerManualProbe : MonoBehaviour
    {
        [ContextMenu("Camera Presentation/Compose Runtime")]
        private void ComposeRuntime()
        {
            CameraPresentationManualRegistry registry = new CameraPresentationManualRegistry();
            CameraPresentationRuntimeComposer composer = new CameraPresentationRuntimeComposer();

            if (!composer.TryCompose(
                registry,
                out CameraPresentationRuntimeCompositionResult result,
                out string reason))
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][CameraPresentation][ComposerProbe] ComposeFailed " +
                    $"reason='{reason}' " +
                    $"resultReason='{result?.Reason}' " +
                    $"directorRegistered='{result?.DirectorRegistered}' " +
                    $"preparationExecutorRegistered='{result?.PreparationExecutorRegistered}'.");

                return;
            }

            bool directorRegistered = registry.IsRegistered<IActivityCameraDirector>();
            bool executorRegistered = registry.IsRegistered<IActivityCameraPreparationExecutor>();

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][ComposerProbe] ComposeSucceeded " +
                $"reason='{reason}' " +
                $"resultReason='{result.Reason}' " +
                $"directorRegistered='{directorRegistered}' " +
                $"preparationExecutorRegistered='{executorRegistered}'.");
        }
    }
}
