using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    public interface IGameplaySessionRunResetService
    {
        Task AcceptAsync(GameplayRunResetRequest request, CancellationToken ct = default);
    }
}
