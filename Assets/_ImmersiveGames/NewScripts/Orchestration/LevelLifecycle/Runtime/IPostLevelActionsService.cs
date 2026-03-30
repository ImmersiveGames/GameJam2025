using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    /// <summary>
    /// Serviço de domínio para ações pós-level (ADR-0027).
    /// </summary>
    public interface IPostLevelActionsService
    {
        Task RestartFromFirstLevelAsync(string reason = null, CancellationToken ct = default);
        Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default);
        Task RestartLevelAsync(string reason = null, CancellationToken ct = default);
        Task NextLevelAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
    }
}
