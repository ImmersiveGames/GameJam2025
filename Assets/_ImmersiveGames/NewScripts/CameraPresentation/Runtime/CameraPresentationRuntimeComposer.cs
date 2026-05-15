using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;

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
                    reason);

                return false;
            }

            IActivityCameraDirector director = new CinemachineActivityCameraDirector();

            if (!registry.TryRegister<IActivityCameraDirector>(director, out reason))
            {
                result = CameraPresentationRuntimeCompositionResult.Failed(
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
                    reason);

                return false;
            }

            reason = "camera_presentation_runtime_composed";
            result = CameraPresentationRuntimeCompositionResult.Completed(reason);
            return true;
        }
    }
}
