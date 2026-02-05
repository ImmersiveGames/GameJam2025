using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IWorldResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
