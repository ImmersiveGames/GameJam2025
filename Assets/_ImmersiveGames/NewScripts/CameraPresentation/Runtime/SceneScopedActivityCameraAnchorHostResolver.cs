using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class SceneScopedActivityCameraAnchorHostResolver : IActivityCameraAnchorHostResolver
    {
        private readonly IDependencyProvider dependencyProvider;

        public SceneScopedActivityCameraAnchorHostResolver(IDependencyProvider dependencyProvider)
        {
            this.dependencyProvider = dependencyProvider;
        }

        public bool TryResolve(
            string sceneName,
            out ActivityCameraAnchorHost anchorHost,
            out string reason)
        {
            anchorHost = null;

            if (dependencyProvider == null)
            {
                reason = "activity_camera_anchor_host_resolver_dependency_provider_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                reason = "activity_camera_scene_name_missing";
                return false;
            }

            if (!dependencyProvider.TryGetForScene<ActivityCameraAnchorHost>(sceneName, out var registeredHost) ||
                registeredHost == null)
            {
                reason = "activity_camera_anchor_host_not_registered";
                return false;
            }

            if (!registeredHost.TryValidate(out reason))
            {
                return false;
            }

            anchorHost = registeredHost;
            reason = "activity_camera_anchor_host_resolved_from_scene_scope";
            return true;
        }
    }
}
