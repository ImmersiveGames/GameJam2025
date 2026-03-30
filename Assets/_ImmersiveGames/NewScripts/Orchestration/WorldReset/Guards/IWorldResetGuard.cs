using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IWorldResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
