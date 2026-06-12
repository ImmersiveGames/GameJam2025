using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class CameraPresentationRuntimeComposer
    {
        public bool TryCompose(
            ICameraPresentationRuntimeRegistry registry,
            out CameraPresentationRuntimeCompositionResult result,
            out string reason)
        {
            if (registry == null)
            {
                reason = "camera_presentation_registry_missing";
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    false,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            var dependencyManager = DependencyManager.Instance;
            if (dependencyManager == null)
            {
                reason = "dependency_manager_instance_missing";
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    false,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            if (!dependencyManager.TryGetGlobal<IOperationalCameraProvider>(out var operationalCameraProvider))
            {
                reason = "operational_camera_provider_not_registered";
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    false,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            if (operationalCameraProvider == null)
            {
                reason = "operational_camera_provider_null";
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    false,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            var director = CameraPresentationRuntimeFactory.CreateActivityDirector(operationalCameraProvider);

            if (!registry.TryRegister<IActivityCameraDirector>(director, out reason))
            {
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    false,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            IActivityCameraPreparationExecutor preparationExecutor = new ActivityCameraPreparationExecutor(
                director);

            if (!registry.TryRegister<IActivityCameraPreparationExecutor>(preparationExecutor, out reason))
            {
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    true,
                    false,
                    false,
                    false,
                    reason);

                return false;
            }

            var routeDirector = CameraPresentationRuntimeFactory.CreateRouteDirector(operationalCameraProvider);

            if (!registry.TryRegister<IRouteCameraDirector>(routeDirector, out reason))
            {
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    true,
                    true,
                    false,
                    false,
                    reason);

                return false;
            }

            var routePreparationExecutor = CameraPresentationRuntimeFactory.CreateRoutePreparationExecutor(routeDirector);

            if (!registry.TryRegister<IRouteCameraPreparationExecutor>(routePreparationExecutor, out reason))
            {
                result = CameraPresentationRuntimeCompositionResult.Failed(
                    true,
                    true,
                    true,
                    false,
                    reason);

                return false;
            }

            reason = "camera_presentation_runtime_composed";
            result = CameraPresentationRuntimeCompositionResult.Completed(reason);
            return true;
        }
    }
}
