using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Debug
{
    public sealed class CameraPresentationSmokeProbe : MonoBehaviour
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
        private ActivityCameraPreparationResult activePreparationResult;
        private GameObject activeRigInstance;

        [ContextMenu("Camera Presentation/Smoke/Run Full Smoke")]
        private void RunFullSmoke()
        {
            CleanupActiveCameraIfNeeded("smoke_pre_cleanup");

            if (!TryResolveExecutorFromDependencyManager(out executor, out string resolveReason))
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][CameraPresentation][SmokeProbe] SmokeFailed stage='resolve_runtime' " +
                    $"reason='{resolveReason}' runtimeSource='dependency_manager'.");

                return;
            }

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][SmokeProbe] RuntimeVerified " +
                $"executorType='{executor.GetType().Name}' " +
                $"runtimeSource='dependency_manager'.");

            ActivityCameraBindingCommand prepareCommand = BuildPrepareCommand();

            if (!executor.TryPrepare(
                    prepareCommand,
                    out ActivityCameraPreparationResult preparationResult,
                    out string prepareReason))
            {
                LogPrepareFailure(preparationResult, prepareReason);
                return;
            }

            activePreparationResult = preparationResult;
            activeRigInstance = preparationResult.Handle != null
                ? preparationResult.Handle.CameraRigInstance
                : null;

            LogPrepareSuccess(preparationResult, prepareReason);

            if (!RunForeignReleaseExpectation())
            {
                CleanupActiveCameraIfNeeded("smoke_foreign_release_failure_cleanup");
                return;
            }

            if (!ReleaseActiveCamera("smoke_release_active_camera"))
            {
                CleanupActiveCameraDirectly("smoke_release_failure_direct_cleanup");
                return;
            }

            UnityEngine.Debug.Log(
                "[OBS][CameraPresentation][SmokeProbe] SmokeSucceeded runtimeSource='dependency_manager'.");
        }

        [ContextMenu("Camera Presentation/Smoke/Cleanup Active Camera")]
        private void CleanupActiveCameraFromContextMenu()
        {
            CleanupActiveCameraIfNeeded("manual_context_cleanup");
        }

        private bool TryResolveExecutorFromDependencyManager(
            out IActivityCameraPreparationExecutor resolvedExecutor,
            out string reason)
        {
            resolvedExecutor = null;

            DependencyManager dependencyManager = DependencyManager.Instance;
            if (dependencyManager == null)
            {
                reason = "dependency_manager_instance_missing";
                return false;
            }

            if (!dependencyManager.TryGetGlobal<IActivityCameraPreparationExecutor>(out resolvedExecutor))
            {
                reason = "activity_camera_preparation_executor_not_registered";
                return false;
            }

            if (resolvedExecutor == null)
            {
                reason = "activity_camera_preparation_executor_null";
                return false;
            }

            reason = "resolved";
            return true;
        }

        private ActivityCameraBindingCommand BuildPrepareCommand()
        {
            ActivityCameraRequirement requirement = new ActivityCameraRequirement(
                requirementId,
                cameraRigPrefab,
                trackingTarget,
                lookAtTarget,
                ActivityCameraActivationTiming.BeforeReveal);

            return new ActivityCameraBindingCommand(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                activityIdentity,
                requirement,
                nameof(CameraPresentationSmokeProbe),
                "camera_presentation_smoke_prepare_activity_camera");
        }

        private ActivityCameraReleaseCommand BuildReleaseCommand(string sourceReason)
        {
            return new ActivityCameraReleaseCommand(
                activePreparationResult.ReadyFact.RouteIdentity,
                activePreparationResult.ReadyFact.RouteOperationId,
                activePreparationResult.ReadyFact.TransitionId,
                activePreparationResult.ReadyFact.RouteSequence,
                activePreparationResult.ReadyFact.ActivityIdentity,
                nameof(CameraPresentationSmokeProbe),
                sourceReason);
        }

        private ActivityCameraReleaseCommand BuildForeignReleaseCommand()
        {
            return new ActivityCameraReleaseCommand(
                "foreign-route",
                activePreparationResult.ReadyFact.RouteOperationId,
                activePreparationResult.ReadyFact.TransitionId,
                activePreparationResult.ReadyFact.RouteSequence,
                activePreparationResult.ReadyFact.ActivityIdentity,
                nameof(CameraPresentationSmokeProbe),
                "camera_presentation_smoke_release_foreign_identity");
        }

        private void LogPrepareSuccess(
            ActivityCameraPreparationResult preparationResult,
            string prepareReason)
        {
            ActivityCameraReadyFact readyFact = preparationResult.ReadyFact;

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][SmokeProbe] ActivityCameraReadyFact " +
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
                $"prepareReason='{prepareReason}' " +
                $"runtimeSource='dependency_manager'.");
        }

        private void LogPrepareFailure(
            ActivityCameraPreparationResult preparationResult,
            string prepareReason)
        {
            ActivityCameraFailureFact failureFact = preparationResult != null
                ? preparationResult.FailureFact
                : null;

            UnityEngine.Debug.LogError(
                $"[OBS][CameraPresentation][SmokeProbe] ActivityCameraFailureFact " +
                $"routeIdentity='{failureFact?.RouteIdentity}' " +
                $"routeOperationId='{failureFact?.RouteOperationId}' " +
                $"transitionId='{failureFact?.TransitionId}' " +
                $"routeSequence='{failureFact?.RouteSequence}' " +
                $"activityIdentity='{failureFact?.ActivityIdentity}' " +
                $"requirementId='{failureFact?.RequirementId}' " +
                $"failureReason='{failureFact?.FailureReason}' " +
                $"source='{failureFact?.Source}' " +
                $"reason='{failureFact?.Reason}' " +
                $"prepareReason='{prepareReason}' " +
                $"runtimeSource='dependency_manager'.");
        }

        private bool RunForeignReleaseExpectation()
        {
            if (executor == null)
            {
                UnityEngine.Debug.LogError(
                    "[OBS][CameraPresentation][SmokeProbe] ForeignReleaseExpectationFailed reason='executor_missing' runtimeSource='dependency_manager'.");

                return false;
            }

            if (activePreparationResult == null)
            {
                UnityEngine.Debug.LogError(
                    "[OBS][CameraPresentation][SmokeProbe] ForeignReleaseExpectationFailed reason='active_preparation_result_missing' runtimeSource='dependency_manager'.");

                return false;
            }

            ActivityCameraReleaseCommand foreignReleaseCommand = BuildForeignReleaseCommand();

            if (executor.TryRelease(
                    foreignReleaseCommand,
                    out ActivityCameraReleaseResult releaseResult,
                    out string reason))
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][CameraPresentation][SmokeProbe] ForeignReleaseUnexpectedlySucceeded " +
                    $"reason='{reason}' expected='foreign_or_stale_camera_release_command' " +
                    $"runtimeSource='dependency_manager'.");

                activePreparationResult = null;
                activeRigInstance = null;
                return false;
            }

            ActivityCameraReleaseFailureFact failureFact = releaseResult != null
                ? releaseResult.FailureFact
                : null;

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][SmokeProbe] ForeignReleaseRejected " +
                $"reason='{reason}' " +
                $"failureReason='{failureFact?.FailureReason}' " +
                $"factReason='{failureFact?.Reason}' " +
                $"expected='foreign_or_stale_camera_release_command' " +
                $"runtimeSource='dependency_manager'.");

            return reason == "foreign_or_stale_camera_release_command";
        }

        private bool ReleaseActiveCamera(string sourceReason)
        {
            if (executor == null)
            {
                UnityEngine.Debug.LogError(
                    "[OBS][CameraPresentation][SmokeProbe] ActivityCameraReleaseFailed reason='executor_missing' runtimeSource='dependency_manager'.");

                return false;
            }

            if (activePreparationResult == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OBS][CameraPresentation][SmokeProbe] ActivityCameraReleaseSkipped reason='active_preparation_result_missing' runtimeSource='dependency_manager'.");

                return true;
            }

            ActivityCameraReleaseCommand releaseCommand = BuildReleaseCommand(sourceReason);

            if (!executor.TryRelease(
                    releaseCommand,
                    out ActivityCameraReleaseResult releaseResult,
                    out string releaseReason))
            {
                ActivityCameraReleaseFailureFact failureFact = releaseResult != null
                    ? releaseResult.FailureFact
                    : null;

                UnityEngine.Debug.LogError(
                    $"[OBS][CameraPresentation][SmokeProbe] ActivityCameraReleaseFailureFact " +
                    $"routeIdentity='{failureFact?.RouteIdentity}' " +
                    $"routeOperationId='{failureFact?.RouteOperationId}' " +
                    $"transitionId='{failureFact?.TransitionId}' " +
                    $"routeSequence='{failureFact?.RouteSequence}' " +
                    $"activityIdentity='{failureFact?.ActivityIdentity}' " +
                    $"failureReason='{failureFact?.FailureReason}' " +
                    $"source='{failureFact?.Source}' " +
                    $"reason='{failureFact?.Reason}' " +
                    $"releaseReason='{releaseReason}' " +
                    $"runtimeSource='dependency_manager'.");

                return false;
            }

            ActivityCameraReleasedFact releasedFact = releaseResult.ReleasedFact;

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][SmokeProbe] ActivityCameraReleasedFact " +
                $"routeIdentity='{releasedFact.RouteIdentity}' " +
                $"routeOperationId='{releasedFact.RouteOperationId}' " +
                $"transitionId='{releasedFact.TransitionId}' " +
                $"routeSequence='{releasedFact.RouteSequence}' " +
                $"activityIdentity='{releasedFact.ActivityIdentity}' " +
                $"source='{releasedFact.Source}' " +
                $"reason='{releasedFact.Reason}' " +
                $"releaseReason='{releaseReason}' " +
                $"runtimeSource='dependency_manager'.");

            activePreparationResult = null;
            activeRigInstance = null;
            return true;
        }

        private void CleanupActiveCameraIfNeeded(string reason)
        {
            if (activePreparationResult != null)
            {
                ReleaseActiveCamera(reason);
                return;
            }

            CleanupActiveCameraDirectly(reason);
        }

        private void CleanupActiveCameraDirectly(string reason)
        {
            if (activeRigInstance == null)
            {
                return;
            }

            GameObject rigToDestroy = activeRigInstance;
            activeRigInstance = null;
            activePreparationResult = null;

            if (Application.isPlaying)
            {
                Destroy(rigToDestroy);
            }
            else
            {
                DestroyImmediate(rigToDestroy);
            }

            UnityEngine.Debug.Log(
                $"[OBS][CameraPresentation][SmokeProbe] DirectCleanupApplied reason='{reason}' runtimeSource='dependency_manager'.");
        }
    }
}
