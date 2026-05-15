using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface IActivityCameraPreparationExecutor
    {
        bool TryPrepare(
            ActivityCameraBindingCommand command,
            out ActivityCameraPreparationResult result,
            out string reason);

        bool TryRelease(
            ActivityCameraReleaseCommand command,
            out string reason);
    }
}
