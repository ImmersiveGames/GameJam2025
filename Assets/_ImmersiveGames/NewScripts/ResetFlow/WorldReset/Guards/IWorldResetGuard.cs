using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Policies;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Guards
{
    /// <summary>
    /// Guard de pré-condições do reset.
    /// </summary>
    public interface IWorldResetGuard
    {
        ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}

