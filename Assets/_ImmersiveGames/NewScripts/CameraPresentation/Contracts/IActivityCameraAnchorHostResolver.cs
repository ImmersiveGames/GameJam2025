using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface IActivityCameraAnchorHostResolver
    {
        bool TryResolve(
            string sceneName,
            out ActivityCameraAnchorHost anchorHost,
            out string reason);
    }
}
