using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface IRouteCameraPreparationExecutor
    {
        bool TryPrepare(
            RouteCameraPresentationCommand command,
            out RouteCameraPresentationResult result,
            out string reason);

        bool TryRelease(
            RouteCameraReleaseCommand command,
            out RouteCameraReleaseResult result,
            out string reason);
    }
}
