namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraBindingResult
    {
        public bool Success { get; }
        public RouteCameraPresentationCommand Command { get; }
        public RouteCameraBindingHandle Handle { get; }
        public string Reason { get; }

        private RouteCameraBindingResult(
            bool success,
            RouteCameraPresentationCommand command,
            RouteCameraBindingHandle handle,
            string reason)
        {
            Success = success;
            Command = command;
            Handle = handle;
            Reason = reason;
        }

        public static RouteCameraBindingResult Ready(
            RouteCameraPresentationCommand command,
            RouteCameraBindingHandle handle,
            string reason)
        {
            return new RouteCameraBindingResult(
                true,
                command,
                handle,
                reason);
        }

        public static RouteCameraBindingResult Failed(
            RouteCameraPresentationCommand command,
            string reason)
        {
            return new RouteCameraBindingResult(
                false,
                command,
                null,
                reason);
        }
    }
}
