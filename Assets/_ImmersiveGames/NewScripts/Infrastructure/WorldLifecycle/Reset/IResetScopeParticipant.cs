using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset
{
    public interface IResetScopeParticipant
    {
        ResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(ResetContext context);
    }
}
