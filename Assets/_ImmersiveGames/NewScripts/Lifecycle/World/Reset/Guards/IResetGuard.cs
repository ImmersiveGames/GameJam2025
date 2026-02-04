using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IResetPolicy policy);
    }
}
