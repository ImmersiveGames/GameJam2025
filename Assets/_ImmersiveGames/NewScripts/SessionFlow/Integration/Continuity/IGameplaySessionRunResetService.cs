using System.Threading;
using System.Threading.Tasks;

namespace ImmersiveGames.GameJam2025.Orchestration.Navigation.Runtime
{
    public interface IGameplaySessionRunResetService
    {
        Task AcceptAsync(GameplayRunResetRequest request, CancellationToken ct = default);
    }
}

