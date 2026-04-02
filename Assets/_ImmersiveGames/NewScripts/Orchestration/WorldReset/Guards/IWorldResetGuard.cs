using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IWorldResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
