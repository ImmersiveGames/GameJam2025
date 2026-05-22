using _ImmersiveGames.NewScripts.CameraPresentation.Models;
namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface IActivityCameraDirector
    {
        bool TryPrepareActivityCamera(
            ActivityCameraBindingCommand command,
            out ActivityCameraBindingResult result,
            out string reason);

        bool TryRebindActivityCameraTargets(
            ActivityCameraBindingHandle bindingHandle,
            ActivityCameraRebindTargetsCommand command,
            out string reason);

        bool TryReleaseActivityCamera(
            ActivityCameraBindingResult binding,
            out string reason);
    }
}
