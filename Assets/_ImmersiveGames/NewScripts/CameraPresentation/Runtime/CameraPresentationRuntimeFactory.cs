using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public static class CameraPresentationRuntimeFactory
    {
        public static IActivityCameraPreparationExecutor CreateDefaultPreparationExecutor(IOperationalCameraProvider operationalCameraProvider)
        {
            IActivityCameraDirector director = new CinemachineActivityCameraDirector(operationalCameraProvider);
            return new ActivityCameraPreparationExecutor(director);
        }
    }
}