using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class SurfaceCameraPresentationRequirementResolver
    {
        public bool TryResolve(
            SurfacePresentationProfileAsset profile,
            SurfaceCameraAnchorHost anchorHost,
            out RouteCameraPresentationRequirement requirement,
            out string reason)
        {
            requirement = null;

            if (profile == null)
            {
                reason = "surface_presentation_profile_missing";
                return false;
            }

            if (!profile.TryValidate(out string profileReason))
            {
                reason = profileReason;
                return false;
            }

            if (profile.RouteCameraPresentationMode == RouteCameraPresentationMode.None)
            {
                reason = "surface_presentation_camera_disabled";
                return false;
            }

            if (anchorHost == null)
            {
                reason = "surface_camera_anchor_host_missing";
                return false;
            }

            if (!anchorHost.TryValidate(out string anchorHostReason))
            {
                reason = anchorHostReason;
                return false;
            }

            if (!anchorHost.TryResolve(
                profile.TrackingAnchorId,
                out var trackingTarget,
                out string trackingReason))
            {
                reason = trackingReason;
                return false;
            }

            Transform lookAtTarget = null;

            if (!string.IsNullOrWhiteSpace(profile.LookAtAnchorId))
            {
                if (!anchorHost.TryResolve(
                    profile.LookAtAnchorId,
                    out lookAtTarget,
                    out string lookAtReason))
                {
                    reason = lookAtReason;
                    return false;
                }
            }

            requirement = new RouteCameraPresentationRequirement(
                BuildRequirementId(profile),
                profile.PresentationRigPrefab,
                trackingTarget,
                lookAtTarget,
                profile.ActivationTiming,
                profile.Priority,
                profile.Required);

            reason = "surface_camera_presentation_requirement_resolved";
            return true;
        }

        private static string BuildRequirementId(
            SurfacePresentationProfileAsset profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                return "surface.presentation.route.camera";
            }

            return $"{profile.ProfileId}.route.camera";
        }
    }
}
