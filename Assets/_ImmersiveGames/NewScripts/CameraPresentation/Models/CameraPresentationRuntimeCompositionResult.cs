namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class CameraPresentationRuntimeCompositionResult
    {
        public bool Success { get; }
        public bool DirectorRegistered { get; }
        public bool PreparationExecutorRegistered { get; }
        public string Reason { get; }

        private CameraPresentationRuntimeCompositionResult(
            bool success,
            bool directorRegistered,
            bool preparationExecutorRegistered,
            string reason)
        {
            Success = success;
            DirectorRegistered = directorRegistered;
            PreparationExecutorRegistered = preparationExecutorRegistered;
            Reason = reason;
        }

        public static CameraPresentationRuntimeCompositionResult Completed(
            string reason)
        {
            return new CameraPresentationRuntimeCompositionResult(
                true,
                true,
                true,
                reason);
        }

        public static CameraPresentationRuntimeCompositionResult Failed(
            bool directorRegistered,
            bool preparationExecutorRegistered,
            string reason)
        {
            return new CameraPresentationRuntimeCompositionResult(
                false,
                directorRegistered,
                preparationExecutorRegistered,
                reason);
        }
    }
}
