using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Authoring
{
    public sealed class SurfaceCameraAnchorHost : MonoBehaviour
    {
        [SerializeField] private SurfaceCameraAnchorBinding[] anchors = Array.Empty<SurfaceCameraAnchorBinding>();

        public bool TryResolve(
            string anchorId,
            out Transform anchor,
            out string reason)
        {
            anchor = null;

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                reason = "surface_camera_anchor_id_missing";
                return false;
            }

            if (anchors == null || anchors.Length == 0)
            {
                reason = "surface_camera_anchor_host_empty";
                return false;
            }

            for (int i = 0; i < anchors.Length; i++)
            {
                SurfaceCameraAnchorBinding binding = anchors[i];

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
                    reason = "surface_camera_anchor_transform_missing";
                    return false;
                }

                anchor = binding.Anchor;
                reason = "surface_camera_anchor_resolved";
                return true;
            }

            reason = "surface_camera_anchor_not_found";
            return false;
        }

        public bool TryValidate(
            out string reason)
        {
            if (anchors == null || anchors.Length == 0)
            {
                reason = "surface_camera_anchor_host_empty";
                return false;
            }

            for (int i = 0; i < anchors.Length; i++)
            {
                SurfaceCameraAnchorBinding binding = anchors[i];

                if (binding == null)
                {
                    reason = $"surface_camera_anchor_binding_null:index={i}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(binding.AnchorId))
                {
                    reason = $"surface_camera_anchor_id_missing:index={i}";
                    return false;
                }

                if (binding.Anchor == null)
                {
                    reason = $"surface_camera_anchor_transform_missing:anchorId={binding.AnchorId}";
                    return false;
                }

                for (int j = i + 1; j < anchors.Length; j++)
                {
                    SurfaceCameraAnchorBinding other = anchors[j];

                    if (other == null)
                    {
                        continue;
                    }

                    if (string.Equals(binding.AnchorId, other.AnchorId, StringComparison.Ordinal))
                    {
                        reason = $"surface_camera_anchor_duplicate:anchorId={binding.AnchorId}";
                        return false;
                    }
                }
            }

            reason = "surface_camera_anchor_host_valid";
            return true;
        }

        [ContextMenu("Camera Presentation/Surface Anchors/Validate")]
        private void ValidateFromContextMenu()
        {
            if (TryValidate(out string reason))
            {
                DebugUtility.Log(
                    typeof(SurfaceCameraAnchorHost),
                    $"[OBS][CameraPresentation][SurfaceAnchorHost] ValidateSucceeded " +
                    $"host='{name}' " +
                    $"anchorCount='{anchors?.Length ?? 0}' " +
                    $"reason='{reason}'.",
                    DebugUtility.Colors.Info);

                return;
            }

            DebugUtility.LogError(
                typeof(SurfaceCameraAnchorHost),
                $"[OBS][CameraPresentation][SurfaceAnchorHost] ValidateFailed " +
                $"host='{name}' " +
                $"reason='{reason}'.");
        }

        [Serializable]
        public sealed class SurfaceCameraAnchorBinding
        {
            [SerializeField] private string anchorId;
            [SerializeField] private Transform anchor;

            public string AnchorId => anchorId;
            public Transform Anchor => anchor;
        }
    }
}
