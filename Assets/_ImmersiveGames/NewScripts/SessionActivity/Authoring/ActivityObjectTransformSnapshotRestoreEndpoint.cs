using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectTransformSnapshotRestoreEndpoint : MonoBehaviour, IActivityObjectSnapshotRestoreEndpoint, IActivityObjectSnapshotRestoreEndpointContractView, IActivityObjectLifecycleContributionProvider
    {
        [SerializeField] private string targetId;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float verificationTolerance = 0.001f;


        public void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions)
        {
            if (contributions == null || !context.IsValid || !Supports(context.TargetId))
            {
                return;
            }

            contributions.Add(new ActivityObjectSnapshotRestoreContribution(
                $"activity_object.snapshot_restore:{context.TargetId}:{nameof(ActivityObjectTransformSnapshotRestoreEndpoint)}",
                300,
                this));
        }

        public bool Supports(string requestedTargetId)
        {
            string local = Normalize(targetId);
            string requested = Normalize(requestedTargetId);
            return !string.IsNullOrWhiteSpace(local) &&
                   !string.IsNullOrWhiteSpace(requested) &&
                   string.Equals(local, requested, StringComparison.Ordinal);
        }

        public ActivityObjectSnapshotRestoreResult ApplyRestore(ActivityObjectSnapshotRestoreCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectSnapshotRestoreCommand is invalid.");
            }

            if (!Supports(command.TargetId))
            {
                var selfPosition = transform.position;
                return new ActivityObjectSnapshotRestoreResult(
                    ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                    command,
                    restoreVerified: false,
                    beforePositionX: selfPosition.x,
                    beforePositionY: selfPosition.y,
                    beforePositionZ: selfPosition.z,
                    afterPositionX: selfPosition.x,
                    afterPositionY: selfPosition.y,
                    afterPositionZ: selfPosition.z,
                    command.Source,
                    command.Reason,
                    $"target_not_supported targetId='{command.TargetId}' endpointTargetId='{Normalize(targetId)}'");
            }

            if (targetTransform == null)
            {
                var selfPosition = transform.position;
                return new ActivityObjectSnapshotRestoreResult(
                    ActivityObjectSnapshotRestoreResultKind.Failed,
                    command,
                    restoreVerified: false,
                    beforePositionX: selfPosition.x,
                    beforePositionY: selfPosition.y,
                    beforePositionZ: selfPosition.z,
                    afterPositionX: selfPosition.x,
                    afterPositionY: selfPosition.y,
                    afterPositionZ: selfPosition.z,
                    command.Source,
                    command.Reason,
                    $"target_transform_missing targetId='{command.TargetId}' contributorPath='{BuildTransformPath(transform)}' restoreEndpointPath='{BuildTransformPath(transform)}' targetTransformPath='<null>'");
            }

            var localTransform = targetTransform;
            var beforePosition = localTransform.position;
            var beforeRotation = localTransform.rotation;
            var beforeScale = localTransform.localScale;

            Vector3 payloadPosition = new(command.PositionX, command.PositionY, command.PositionZ);
            Quaternion payloadRotation = new(command.RotationX, command.RotationY, command.RotationZ, command.RotationW);
            Vector3 payloadScale = new(command.ScaleX, command.ScaleY, command.ScaleZ);

            if (command.CoordinateSpace == ActivityObjectSnapshotCoordinateSpace.WorldTransform)
            {
                localTransform.position = payloadPosition;
                localTransform.rotation = payloadRotation;
                localTransform.localScale = payloadScale;
            }
            else if (command.CoordinateSpace == ActivityObjectSnapshotCoordinateSpace.LocalTransform)
            {
                localTransform.localPosition = payloadPosition;
                localTransform.localRotation = payloadRotation;
                localTransform.localScale = payloadScale;
            }
            else
            {
                return new ActivityObjectSnapshotRestoreResult(
                    ActivityObjectSnapshotRestoreResultKind.Failed,
                    command,
                    restoreVerified: false,
                    beforePosition.x,
                    beforePosition.y,
                    beforePosition.z,
                    localTransform.position.x,
                    localTransform.position.y,
                    localTransform.position.z,
                    command.Source,
                    command.Reason,
                    $"unsupported_coordinate_space coordinateSpace='{command.CoordinateSpace}'");
            }

            var afterPosition = localTransform.position;
            var afterRotation = localTransform.rotation;
            var afterScale = localTransform.localScale;
            bool verified =
                IsNearlyEqual(afterPosition, payloadPosition, verificationTolerance) &&
                IsNearlyEqual(afterRotation, payloadRotation, verificationTolerance) &&
                IsNearlyEqual(afterScale, payloadScale, verificationTolerance);
            string coordinateSpace = command.CoordinateSpace == ActivityObjectSnapshotCoordinateSpace.LocalTransform
                ? "local_transform"
                : "world_transform";
            bool hasRigidbody = localTransform.GetComponent<Rigidbody>() != null;
            bool hasRigidbody2D = localTransform.GetComponent<Rigidbody2D>() != null;

            return new ActivityObjectSnapshotRestoreResult(
                verified ? ActivityObjectSnapshotRestoreResultKind.Restored : ActivityObjectSnapshotRestoreResultKind.Failed,
                command,
                verified,
                beforePosition.x,
                beforePosition.y,
                beforePosition.z,
                afterPosition.x,
                afterPosition.y,
                afterPosition.z,
                command.Source,
                command.Reason,
                $"restore_transform targetId='{command.TargetId}' contributorPath='{BuildTransformPath(transform)}' restoreEndpointPath='{BuildTransformPath(transform)}' targetTransformPath='{BuildTransformPath(targetTransform)}' objectName='{gameObject.name}' coordinateSpace='{coordinateSpace}' hasRigidbody='{hasRigidbody.ToString().ToLowerInvariant()}' hasRigidbody2D='{hasRigidbody2D.ToString().ToLowerInvariant()}' beforePosition='{beforePosition}' beforeRotation='{beforeRotation}' beforeScale='{beforeScale}' payloadPosition='{payloadPosition}' payloadRotation='{payloadRotation}' payloadScale='{payloadScale}' afterPosition='{afterPosition}' afterRotation='{afterRotation}' afterScale='{afterScale}' restoreVerified='{verified.ToString().ToLowerInvariant()}'");
        }

        public bool TryDescribeContract(
            string requestedTargetId,
            out string endpointPath,
            out string targetTransformPath,
            out string failureReason)
        {
            endpointPath = BuildTransformPath(transform);
            targetTransformPath = BuildTransformPath(targetTransform);

            if (!Supports(requestedTargetId))
            {
                failureReason = "target_not_supported";
                return false;
            }

            if (targetTransform == null)
            {
                failureReason = "target_transform_missing";
                return false;
            }

            failureReason = "resolved";
            return true;
        }

        private void OnValidate()
        {
            targetId = Normalize(targetId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsNearlyEqual(Vector3 left, Vector3 right, float tolerance)
        {
            return Mathf.Abs(left.x - right.x) <= tolerance &&
                   Mathf.Abs(left.y - right.y) <= tolerance &&
                   Mathf.Abs(left.z - right.z) <= tolerance;
        }

        private static bool IsNearlyEqual(Quaternion left, Quaternion right, float tolerance)
        {
            return Mathf.Abs(left.x - right.x) <= tolerance &&
                   Mathf.Abs(left.y - right.y) <= tolerance &&
                   Mathf.Abs(left.z - right.z) <= tolerance &&
                   Mathf.Abs(left.w - right.w) <= tolerance;
        }

        private static string BuildTransformPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            var current = target.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
