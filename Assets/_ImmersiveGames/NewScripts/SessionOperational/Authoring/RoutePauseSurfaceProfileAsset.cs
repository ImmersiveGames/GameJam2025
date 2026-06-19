using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Authoring
{
    [CreateAssetMenu(
        fileName = "RoutePauseSurfaceProfile",
        menuName = "ImmersiveGames/Session Operational/Route Pause Surface Profile",
        order = 45)]
    public sealed class RoutePauseSurfaceProfileAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string surfaceId = "route.pause.surface.default";

        [Header("Surface")]
        [SerializeField] private RoutePauseSurfaceMode mode = RoutePauseSurfaceMode.None;
        [SerializeField] private RoutePauseSurfacePreloadPolicy preloadPolicy = RoutePauseSurfacePreloadPolicy.None;
        [SerializeField] private SceneKeyAsset surfaceScene;

        [Header("Roots")]
        [SerializeField] private string overlayRootId = "pause.overlay.root";
        [SerializeField] private string activityContentRootId = "pause.activity.content.root";

        [Header("Policy")]
        [SerializeField] private bool required;

        [Header("Input")]
        [SerializeField] private SessionOperationalInputModeKind inputModeOnPause = SessionOperationalInputModeKind.PauseOverlay;
        [SerializeField] private SessionOperationalInputPolicy inputPolicyOnPause = SessionOperationalInputPolicy.OverlayNavigation;
        [SerializeField] private SessionOperationalInputModeKind inputModeOnResume = SessionOperationalInputModeKind.ActivityDefault;
        [SerializeField] private SessionOperationalInputPolicy inputPolicyOnResume = SessionOperationalInputPolicy.ActivityGameplay;

        public string SurfaceId => surfaceId.TrimToEmpty();
        public RoutePauseSurfaceMode Mode => mode;
        public RoutePauseSurfacePreloadPolicy PreloadPolicy => preloadPolicy;
        public SceneKeyAsset SurfaceScene => surfaceScene;
        public string OverlayRootId => overlayRootId.TrimToEmpty();
        public string ActivityContentRootId => activityContentRootId.TrimToEmpty();
        public bool Required => required;
        public SessionOperationalInputModeKind InputModeOnPause => inputModeOnPause;
        public SessionOperationalInputPolicy InputPolicyOnPause => inputPolicyOnPause;
        public SessionOperationalInputModeKind InputModeOnResume => inputModeOnResume;
        public SessionOperationalInputPolicy InputPolicyOnResume => inputPolicyOnResume;

        public RoutePauseSurfaceProfile ToProfile()
        {
            return new RoutePauseSurfaceProfile(
                new RoutePauseSurfaceId(SurfaceId),
                mode,
                preloadPolicy,
                surfaceScene,
                new RoutePauseSurfaceRootId(OverlayRootId),
                new RoutePauseSurfaceRootId(ActivityContentRootId),
                required,
                inputModeOnPause,
                inputPolicyOnPause,
                inputModeOnResume,
                inputPolicyOnResume);
        }

        public bool TryValidate(out string reason)
        {
            var profile = ToProfile();
            if (profile.IsValid)
            {
                reason = profile.IsDisabled
                    ? "route_pause_surface_disabled"
                    : "route_pause_surface_profile_valid";
                return true;
            }

            if (string.IsNullOrWhiteSpace(SurfaceId))
            {
                reason = "route_pause_surface_id_missing";
                return false;
            }

            if (mode == RoutePauseSurfaceMode.None)
            {
                if (required)
                {
                    reason = "route_pause_surface_disabled_required_invalid";
                    return false;
                }

                reason = preloadPolicy == RoutePauseSurfacePreloadPolicy.None
                    ? "route_pause_surface_disabled"
                    : "route_pause_surface_disabled_preload_policy_invalid";
                return preloadPolicy == RoutePauseSurfacePreloadPolicy.None;
            }

            if (mode != RoutePauseSurfaceMode.AdditiveScene)
            {
                reason = "route_pause_surface_mode_invalid";
                return false;
            }

            if (preloadPolicy != RoutePauseSurfacePreloadPolicy.PreloadWithRoute)
            {
                reason = "route_pause_surface_preload_policy_invalid";
                return false;
            }

            if (surfaceScene == null || string.IsNullOrWhiteSpace(surfaceScene.SceneName))
            {
                reason = "route_pause_surface_scene_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(OverlayRootId))
            {
                reason = "route_pause_surface_overlay_root_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ActivityContentRootId))
            {
                reason = "route_pause_surface_activity_content_root_missing";
                return false;
            }

            if (inputModeOnPause != SessionOperationalInputModeKind.PauseOverlay ||
                inputPolicyOnPause != SessionOperationalInputPolicy.OverlayNavigation)
            {
                reason = "route_pause_surface_pause_input_invalid";
                return false;
            }

            if (inputModeOnResume != SessionOperationalInputModeKind.ActivityDefault ||
                inputPolicyOnResume != SessionOperationalInputPolicy.ActivityGameplay)
            {
                reason = "route_pause_surface_resume_input_invalid";
                return false;
            }

            reason = "route_pause_surface_profile_invalid";
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            surfaceId = surfaceId.TrimToEmpty();
            overlayRootId = overlayRootId.TrimToEmpty();
            activityContentRootId = activityContentRootId.TrimToEmpty();

            if (TryValidate(out string reason))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(RoutePauseSurfaceProfileAsset),
                $"[Config][Editor][RoutePauseSurface] surfaceId='{SurfaceId}' invalid. reason='{reason}'");
        }
#endif
    }
}
