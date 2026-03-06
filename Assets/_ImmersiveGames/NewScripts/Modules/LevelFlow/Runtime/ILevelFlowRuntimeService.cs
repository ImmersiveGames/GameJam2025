using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelFlowRuntimeService
    {
        [System.Obsolete("Legacy API. Use StartGameplayDefaultAsync(reason, ct).")]
        Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default);
        Task StartGameplayDefaultAsync(string reason = null, CancellationToken ct = default);
        Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default);
        Task SwapLevelLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default);
    }
}
