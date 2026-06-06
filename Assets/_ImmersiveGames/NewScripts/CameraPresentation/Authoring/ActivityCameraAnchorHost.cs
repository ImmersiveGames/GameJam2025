using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Authoring
{
    public sealed class ActivityCameraAnchorHost : MonoBehaviour
    {
        [SerializeField] private ActivityCameraAnchorBinding[] anchors =
            Array.Empty<ActivityCameraAnchorBinding>();

        private void Awake()
        {
            RegisterSceneScopedOrFail();
        }

        public bool TryResolve(
            string anchorId,
            out Transform anchor,
            out string reason)
        {
            anchor = null;

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                reason = "activity_camera_anchor_id_missing";
                return false;
            }

            if (anchors == null || anchors.Length == 0)
            {
                reason = "activity_camera_anchor_host_empty";
                return false;
            }

            for (int i = 0; i < anchors.Length; i++)
            {
                ActivityCameraAnchorBinding binding = anchors[i];

                if (binding == null)
                {
                    continue;
                }

                if (!string.Equals(binding.AnchorId, anchorId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (binding.Anchor == null)
                {
                    reason = "activity_camera_anchor_transform_missing";
                    return false;
                }

                anchor = binding.Anchor;
                reason = "activity_camera_anchor_resolved";
                return true;
            }

            reason = "activity_camera_anchor_not_found";
            return false;
        }

        public bool TryValidate(
            out string reason)
        {
            if (anchors == null || anchors.Length == 0)
            {
                reason = "activity_camera_anchor_host_empty";
                return false;
            }

            for (int i = 0; i < anchors.Length; i++)
            {
                ActivityCameraAnchorBinding binding = anchors[i];

                if (binding == null)
                {
                    reason = $"activity_camera_anchor_binding_null:index={i}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(binding.AnchorId))
                {
                    reason = $"activity_camera_anchor_id_missing:index={i}";
                    return false;
                }

                if (binding.Anchor == null)
                {
                    reason = $"activity_camera_anchor_transform_missing:anchorId={binding.AnchorId}";
                    return false;
                }

                for (int j = i + 1; j < anchors.Length; j++)
                {
                    ActivityCameraAnchorBinding other = anchors[j];

                    if (other == null)
                    {
                        continue;
                    }

                    if (string.Equals(binding.AnchorId, other.AnchorId, StringComparison.Ordinal))
                    {
                        reason = $"activity_camera_anchor_duplicate:anchorId={binding.AnchorId}";
                        return false;
                    }
                }
            }

            reason = "activity_camera_anchor_host_valid";
            return true;
        }

        [ContextMenu("Camera Presentation/Activity Anchors/Validate")]
        private void ValidateFromContextMenu()
        {
            if (TryValidate(out string reason))
            {
                DebugUtility.Log(
                    typeof(ActivityCameraAnchorHost),
                    $"[OBS][CameraPresentation][ActivityAnchorHost] ValidateSucceeded " +
                    $"host='{name}' " +
                    $"anchorCount='{anchors?.Length ?? 0}' " +
                    $"reason='{reason}'.",
                    DebugUtility.Colors.Info);

                return;
            }

            DebugUtility.LogError(
                typeof(ActivityCameraAnchorHost),
                $"[OBS][CameraPresentation][ActivityAnchorHost] ValidateFailed " +
                $"host='{name}' " +
                $"reason='{reason}'.");
        }

        private void RegisterSceneScopedOrFail()
        {
            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraPresentation][ActivityAnchorHost] DependencyManager obrigatorio ausente para registrar o host por cena.");
            }

            if (!TryValidate(out string validationReason))
            {
                throw new InvalidOperationException($"[FATAL][Config][CameraPresentation][ActivityAnchorHost] Host invalido ao registrar por cena host='{name}' reason='{validationReason}'.");
            }

            string sceneName = gameObject.scene.name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"[FATAL][Config][CameraPresentation][ActivityAnchorHost] Scene name obrigatorio ausente ao registrar host='{name}'.");
            }

            if (DependencyManager.Provider.TryGetForScene<ActivityCameraAnchorHost>(sceneName, out var existingHost) &&
                existingHost != null &&
                !ReferenceEquals(existingHost, this))
            {
                throw new InvalidOperationException($"[FATAL][Config][CameraPresentation][ActivityAnchorHost] Duplicate ActivityCameraAnchorHost scene='{sceneName}' existingHost='{existingHost.name}' newHost='{name}'.");
            }

            if (ReferenceEquals(existingHost, this))
            {
                return;
            }

            DependencyManager.Provider.RegisterForScene(sceneName, this, allowOverride: false);

            DebugUtility.Log(
                typeof(ActivityCameraAnchorHost),
                $"[OBS][CameraPresentation][ActivityAnchorHost] Registered scene-scoped host scene='{sceneName}' host='{name}' anchorCount='{anchors?.Length ?? 0}'.",
                DebugUtility.Colors.Info);
        }

        [Serializable]
        public sealed class ActivityCameraAnchorBinding
        {
            [SerializeField] private string anchorId;
            [SerializeField] private Transform anchor;

            public string AnchorId => anchorId;
            public Transform Anchor => anchor;
        }
    }
}
