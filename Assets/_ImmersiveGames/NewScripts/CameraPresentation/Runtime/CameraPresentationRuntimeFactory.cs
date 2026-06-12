using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public static class CameraPresentationRuntimeFactory
    {
        public static IActivityCameraDirector CreateActivityDirector(IOperationalCameraProvider operationalCameraProvider)
        {
            return new CinemachineActivityCameraDirector(operationalCameraProvider);
        }

        public static IActivityCameraPreparationExecutor CreateDefaultPreparationExecutor(IOperationalCameraProvider operationalCameraProvider)
        {
            var director = CreateActivityDirector(operationalCameraProvider);
            return new ActivityCameraPreparationExecutor(director);
        }

        public static IRouteCameraDirector CreateRouteDirector(IOperationalCameraProvider operationalCameraProvider)
        {
            return new CinemachineRouteCameraDirector(operationalCameraProvider);
        }

        public static IRouteCameraPreparationExecutor CreateRoutePreparationExecutor(IOperationalCameraProvider operationalCameraProvider)
        {
            var director = CreateRouteDirector(operationalCameraProvider);
            return CreateRoutePreparationExecutor(director);
        }

        public static IRouteCameraPreparationExecutor CreateRoutePreparationExecutor(IRouteCameraDirector director)
        {
            var validator = new RouteCameraPresentationCommandValidator();
            return new RouteCameraPreparationExecutor(director, validator);
        }
    }
}
