using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectTransformSnapshotProvider : MonoBehaviour, IActivityObjectSnapshotProvider
    {
        [SerializeField] private string targetId;

        public bool Supports(string requestedTargetId)
        {
            string local = Normalize(targetId);
            string requested = Normalize(requestedTargetId);
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
                    $"target_not_supported targetId='{command.TargetId}' providerTargetId='{Normalize(targetId)}'");
            }

            Transform localTransform = transform;
            Vector3 position = localTransform.position;
            Quaternion rotation = localTransform.rotation;
            Vector3 scale = localTransform.localScale;
            ActivityObjectSnapshot snapshot = new(
                command.Identity,
                command.ContentProfileId,
                command.TargetId,
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
                $"captured_transform targetId='{command.TargetId}' position='{position}' rotation='{rotation}' scale='{scale}'");
        }

        private void OnValidate()
        {
            targetId = Normalize(targetId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
