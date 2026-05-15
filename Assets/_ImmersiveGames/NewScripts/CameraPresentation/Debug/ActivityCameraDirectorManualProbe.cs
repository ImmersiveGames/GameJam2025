using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Debug
{
    public sealed class ActivityCameraDirectorManualProbe : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string routeIdentity = "manual-route";
        [SerializeField] private string routeOperationId = "manual-route-operation";
        [SerializeField] private string transitionId = "manual-transition";
        [SerializeField] private int routeSequence = 1;
        [SerializeField] private string activityIdentity = "manual-activity";

        [Header("Requirement")]
        [SerializeField] private string requirementId = "manual-camera-requirement";
        [SerializeField] private GameObject cameraRigPrefab;
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private Transform lookAtTarget;

        private IActivityCameraPreparationExecutor executor;
        private ActivityCameraPreparationResult lastPreparationResult;
        private GameObject lastRigInstance;

        [ContextMenu("Camera Presentation/Prepare Activity Camera")]
        private void PrepareActivityCamera()
        {
            CleanupLastRig();

            ActivityCameraRequirement requirement = new ActivityCameraRequirement(
                requirementId,
                cameraRigPrefab,
                trackingTarget,
                lookAtTarget,
                ActivityCameraActivationTiming.BeforeReveal);

            ActivityCameraBindingCommand command = new ActivityCameraBindingCommand(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                activityIdentity,
                requirement,
                nameof(ActivityCameraDirectorManualProbe),
                "manual_probe_prepare_activity_camera");

            executor = CameraPresentationRuntimeFactory.CreateDefaultPreparationExecutor();

            if (!executor.TryPrepare(command, out ActivityCameraPreparationResult preparationResult, out string prepareReason))
            {
                ActivityCameraFailureFact failureFact = preparationResult?.FailureFact;

                if (failureFact == null)
                {
                    UnityEngine.Debug.LogError(
                        $"[OBS][CameraPresentation][ManualProbe] ActivityCameraFailureFactMissing " +
                        $"reason='{prepareReason}'.");

                    return;
                }

                UnityEngine.Debug.LogError(
                    $"[OBS][CameraPresentation][ManualProbe] ActivityCameraFailureFact " +
                    $"routeIdentity='{failureFact.RouteIdentity}' " +
                    $"routeOperationId='{failureFact.RouteOperationId}' " +
                    $"transitionId='{failureFact.TransitionId}' " +
                    $"routeSequence='{failureFact.RouteSequence}' " +
                    $"activityIdentity='{failureFact.ActivityIdentity}' " +
                    $"requirementId='{failureFact.RequirementId}' " +
                    $"failureReason='{failureFact.FailureReason}' " +
                    $"source='{failureFact.Source}' " +
                    $"reason='{failureFact.Reason}' " +
                    $"prepareReason='{prepareReason}'.");

                return;
            }

            lastPreparationResult = preparationResult;
            lastRigInstance = preparationResult.Handle.CameraRigInstance;

            ActivityCameraReadyFact readyFact = preparationResult.ReadyFact;

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][ManualProbe] ActivityCameraReadyFact " +
                $"routeIdentity='{readyFact.RouteIdentity}' " +
                $"routeOperationId='{readyFact.RouteOperationId}' " +
                $"transitionId='{readyFact.TransitionId}' " +
                $"routeSequence='{readyFact.RouteSequence}' " +
                $"activityIdentity='{readyFact.ActivityIdentity}' " +
                $"requirementId='{readyFact.RequirementId}' " +
                $"camera='{readyFact.Handle.UnityCamera.name}' " +
                $"rig='{readyFact.Handle.CameraRigInstance.name}' " +
                $"source='{readyFact.Source}' " +
                $"reason='{readyFact.Reason}' " +
                $"prepareReason='{prepareReason}'.");
        }

        [ContextMenu("Camera Presentation/Cleanup Last Rig")]
        private void CleanupLastRig()
        {
            if (lastPreparationResult == null)
            {
                CleanupLastRigInstanceDirectly();
                return;
            }

            if (executor == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OBS][CameraPresentation][ManualProbe] ActivityCameraReleaseSkipped reason='executor_missing'. Falling back to direct cleanup.");

                CleanupLastRigInstanceDirectly();
                lastPreparationResult = null;
                return;
            }

            ActivityCameraReleaseCommand releaseCommand = new ActivityCameraReleaseCommand(
                lastPreparationResult.ReadyFact.RouteIdentity,
                lastPreparationResult.ReadyFact.RouteOperationId,
                lastPreparationResult.ReadyFact.TransitionId,
                lastPreparationResult.ReadyFact.RouteSequence,
                lastPreparationResult.ReadyFact.ActivityIdentity,
                nameof(ActivityCameraDirectorManualProbe),
                "manual_probe_release_activity_camera");

            if (!executor.TryRelease(releaseCommand, out string releaseReason))
            {
                UnityEngine.Debug.LogWarning(
                    $"[OBS][CameraPresentation][ManualProbe] ActivityCameraReleaseFailed reason='{releaseReason}'. Falling back to direct cleanup.");

                CleanupLastRigInstanceDirectly();
                lastPreparationResult = null;
                return;
            }

            lastRigInstance = null;
            lastPreparationResult = null;

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][ManualProbe] ActivityCameraReleaseSucceeded reason='{releaseReason}'.");
        }

        private void CleanupLastRigInstanceDirectly()
        {
            if (lastRigInstance == null)
            {
                return;
            }

            GameObject rigToDestroy = lastRigInstance;
            lastRigInstance = null;

            if (Application.isPlaying)
            {
                Destroy(rigToDestroy);
            }
            else
            {
                DestroyImmediate(rigToDestroy);
            }

            UnityEngine.Debug.Log("[OBS][CameraPresentation][ManualProbe] Last activity camera rig cleaned directly.");
        }

        [ContextMenu("Camera Presentation/Release With Foreign Identity")]
        private void ReleaseWithForeignIdentity()
        {
            if (lastPreparationResult == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OBS][CameraPresentation][ManualProbe] ForeignReleaseSkipped reason='no_active_preparation_result'.");

                return;
            }

            if (executor == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OBS][CameraPresentation][ManualProbe] ForeignReleaseSkipped reason='executor_missing'.");

                return;
            }

            ActivityCameraReleaseCommand foreignReleaseCommand = new ActivityCameraReleaseCommand(
                "foreign-route",
                lastPreparationResult.ReadyFact.RouteOperationId,
                lastPreparationResult.ReadyFact.TransitionId,
                lastPreparationResult.ReadyFact.RouteSequence,
                lastPreparationResult.ReadyFact.ActivityIdentity,
                nameof(ActivityCameraDirectorManualProbe),
                "manual_probe_release_foreign_identity");

            if (!executor.TryRelease(foreignReleaseCommand, out string reason))
            {
                UnityEngine.Debug.LogWarning(
                    $"[OBS][CameraPresentation][ManualProbe] ForeignReleaseRejected reason='{reason}' expected='foreign_or_stale_camera_release_command'.");

                return;
            }

            UnityEngine.Debug.LogError(
                "[OBS][CameraPresentation][ManualProbe] ForeignReleaseUnexpectedlySucceeded expected='foreign_or_stale_camera_release_command'.");
        }
    }
}
