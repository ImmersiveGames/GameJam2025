using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public static class CameraPresentationRuntimeFactory
    {
        public static IActivityCameraPreparationExecutor CreateDefaultPreparationExecutor()
        {
            IActivityCameraDirector director = new CinemachineActivityCameraDirector();
            return new ActivityCameraPreparationExecutor(director);
        }
    }
}
