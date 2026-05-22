using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface IRouteCameraDirector
    {
        bool TryPrepareRouteCamera(
            RouteCameraPresentationCommand command,
            out RouteCameraBindingResult result,
            out string reason);

        bool TryReleaseRouteCamera(
            RouteCameraBindingResult activeBinding,
            out string reason);
    }
}
