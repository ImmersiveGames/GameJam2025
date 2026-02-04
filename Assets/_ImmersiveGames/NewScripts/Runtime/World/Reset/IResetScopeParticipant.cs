using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Runtime.World.Reset
{
    public interface IResetScopeParticipant
    {
        ResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(ResetContext context);
    }
}

