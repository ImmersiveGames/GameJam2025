using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Serviço de domínio para ações pós-level (ADR-0027).
    /// </summary>
    public interface IPostLevelActionsService
    {
        Task RestartLevelAsync(string reason = null, CancellationToken ct = default);
        Task NextLevelAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
    }
}
