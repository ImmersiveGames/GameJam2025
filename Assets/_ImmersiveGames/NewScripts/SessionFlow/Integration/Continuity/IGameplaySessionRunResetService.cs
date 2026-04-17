using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    public interface IGameplaySessionRunResetService
    {
        Task AcceptAsync(GameplayRunResetRequest request, CancellationToken ct = default);
    }
}

