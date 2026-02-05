using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Interop
{
    public interface IRunRearmWorldParticipant
    {
        WorldResetScope Scope { get; }

        int Order { get; }

        Task ResetAsync(WorldResetContext context);
    }
}

