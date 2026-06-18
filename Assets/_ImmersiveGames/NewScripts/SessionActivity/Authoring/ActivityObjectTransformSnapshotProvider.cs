using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectTransformSnapshotProvider : MonoBehaviour, IActivityObjectSnapshotProvider, IActivityObjectLifecycleContributionProvider
    {
        [SerializeField] private string targetId;
        [SerializeField] private Transform targetTransform;


        public void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions)
        {
            if (contributions == null || !context.IsValid || !Supports(context.TargetId))
            {
                return;
            }

            contributions.Add(new ActivityObjectSnapshotContribution(
                $"activity_object.snapshot:{context.TargetId}:{nameof(ActivityObjectTransformSnapshotProvider)}",
                200,
                this));
        }

        public bool Supports(string requestedTargetId)
        {
            string local = targetId.TrimToEmpty();
            string requested = requestedTargetId.TrimToEmpty();
            return !string.IsNullOrWhiteSpace(local) &&
                   !string.IsNullOrWhiteSpace(requested) &&
                   string.Equals(local, requested, StringComparison.Ordinal);
        }

        public ActivityObjectSnapshotCaptureResult CaptureSnapshot(ActivityObjectSnapshotCaptureCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectSnapshotCaptureCommand is invalid.");
            }

            if (!Supports(command.TargetId))
            {
                return new ActivityObjectSnapshotCaptureResult(
                    ActivityObjectSnapshotCaptureResultKind.SkippedOptional,
                    command,
                    default,
                    false,
                    command.Source,
                    command.Reason,
                    $"target_not_supported targetId='{command.TargetId}' providerTargetId='{targetId.TrimToEmpty()}'");
            }

            if (targetTransform == null)
            {
                return new ActivityObjectSnapshotCaptureResult(
                    ActivityObjectSnapshotCaptureResultKind.Failed,
                    command,
                    default,
                    false,
                    command.Source,
                    command.Reason,
                    $"target_transform_missing targetId='{command.TargetId}' contributorPath='{BuildTransformPath(transform)}' providerPath='{BuildTransformPath(transform)}' targetTransformPath='<null>'");
            }

            var localTransform = targetTransform;
            var position = localTransform.position;
            var rotation = localTransform.rotation;
            var scale = localTransform.localScale;
            ActivityObjectSnapshot snapshot = new(
                command.Identity,
                command.ContentProfileId,
                command.TargetId,
                ActivityObjectSnapshotCoordinateSpace.WorldTransform,
                position.x,
                position.y,
                position.z,
                rotation.x,
                rotation.y,
                rotation.z,
                rotation.w,
                scale.x,
                scale.y,
                scale.z,
                command.Source,
                command.Reason);

            return new ActivityObjectSnapshotCaptureResult(
                ActivityObjectSnapshotCaptureResultKind.Captured,
                command,
                snapshot,
                hasTransformPayload: true,
                command.Source,
                command.Reason,
                $"captured_transform targetId='{command.TargetId}' contributorPath='{BuildTransformPath(transform)}' providerPath='{BuildTransformPath(transform)}' targetTransformPath='{BuildTransformPath(targetTransform)}' coordinateSpace='world_transform' capturedPosition='({position.x:0.###},{position.y:0.###},{position.z:0.###})' position='{position}' rotation='{rotation}' scale='{scale}'");
        }

        private void OnValidate()
        {
            targetId = targetId.TrimToEmpty();
        }
private static string BuildTransformPath(Transform current)
        {
            if (current == null)
            {
                return "<null>";
            }

            string path = current.name;
            var node = current.parent;
            while (node != null)
            {
                path = $"{node.name}/{path}";
                node = node.parent;
            }

            return path;
        }
    }
}
