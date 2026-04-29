using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    public enum WorldResetLocalExecutionStatus
    {
        Completed = 0,
        Rejected = 1,
        Unconfirmed = 2,
        Failed = 3
    }

    public readonly struct WorldResetLocalExecutionResult
    {
        private WorldResetLocalExecutionResult(
            WorldResetLocalExecutionStatus status,
            string sceneName,
            string reason,
            string source,
            string detail)
        {
            Status = status;
            SceneName = Normalize(sceneName);
            Reason = Normalize(reason);
            Source = Normalize(source);
            Detail = Normalize(detail);
        }

        public WorldResetLocalExecutionStatus Status { get; }
        public string SceneName { get; }
        public string Reason { get; }
        public string Source { get; }
        public string Detail { get; }
        public bool Succeeded => Status == WorldResetLocalExecutionStatus.Completed;

        public static WorldResetLocalExecutionResult Completed(string sceneName, string reason, string source, string detail = null)
        {
            return new WorldResetLocalExecutionResult(
                WorldResetLocalExecutionStatus.Completed,
                sceneName,
                reason,
                source,
                string.IsNullOrWhiteSpace(detail) ? "Local world reset completed." : detail);
        }

        public static WorldResetLocalExecutionResult Rejected(string sceneName, string reason, string source, string detail)
        {
            return new WorldResetLocalExecutionResult(
                WorldResetLocalExecutionStatus.Rejected,
                sceneName,
                reason,
                source,
                detail);
        }

        public static WorldResetLocalExecutionResult Unconfirmed(string sceneName, string reason, string source, string detail)
        {
            return new WorldResetLocalExecutionResult(
                WorldResetLocalExecutionStatus.Unconfirmed,
                sceneName,
                reason,
                source,
                detail);
        }

        public static WorldResetLocalExecutionResult Failed(string sceneName, string reason, string source, string detail)
        {
            return new WorldResetLocalExecutionResult(
                WorldResetLocalExecutionStatus.Failed,
                sceneName,
                reason,
                source,
                detail);
        }

        public override string ToString()
        {
            return $"Status='{Status}', Succeeded='{Succeeded}', SceneName='{SceneName}', Reason='{Reason}', Source='{Source}', Detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Boundary neutro para execucao local/material do reset.
    /// A implementacao concreta pode morar em SceneReset ou em outro executor local.
    /// </summary>
    public interface IWorldResetLocalExecutor
    {
        Task<WorldResetLocalExecutionResult> ResetWorldAsync(string reason);
    }
}
