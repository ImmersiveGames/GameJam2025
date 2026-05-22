namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class CameraPresentationRuntimeCompositionResult
    {
        public bool Success { get; }
        public bool DirectorRegistered { get; }
        public bool PreparationExecutorRegistered { get; }
        public bool RouteDirectorRegistered { get; }
        public bool RoutePreparationExecutorRegistered { get; }
        public string Reason { get; }

        private CameraPresentationRuntimeCompositionResult(
            bool success,
            bool directorRegistered,
            bool preparationExecutorRegistered,
            bool routeDirectorRegistered,
            bool routePreparationExecutorRegistered,
            string reason)
        {
            Success = success;
            DirectorRegistered = directorRegistered;
            PreparationExecutorRegistered = preparationExecutorRegistered;
            RouteDirectorRegistered = routeDirectorRegistered;
            RoutePreparationExecutorRegistered = routePreparationExecutorRegistered;
            Reason = reason;
        }

        public static CameraPresentationRuntimeCompositionResult Completed(
            string reason)
        {
            return new CameraPresentationRuntimeCompositionResult(
                true,
                true,
                true,
                true,
                true,
                reason);
        }

        public static CameraPresentationRuntimeCompositionResult Failed(
            bool directorRegistered,
            bool preparationExecutorRegistered,
            bool routeDirectorRegistered,
            bool routePreparationExecutorRegistered,
            string reason)
        {
            return new CameraPresentationRuntimeCompositionResult(
                false,
                directorRegistered,
                preparationExecutorRegistered,
                routeDirectorRegistered,
                routePreparationExecutorRegistered,
                reason);
        }
    }
}
