using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Validation
{
    /// <summary>
    /// Validador do request de reset.
    /// </summary>
    public interface IWorldResetValidator
    {
        ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
