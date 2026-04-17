using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Policies;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Validation
{
    /// <summary>
    /// Validador do request de reset.
    /// </summary>
    public interface IWorldResetValidator
    {
        ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}

