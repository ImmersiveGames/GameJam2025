using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IWorldResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}

