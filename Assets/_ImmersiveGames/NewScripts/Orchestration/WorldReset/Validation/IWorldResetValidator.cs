using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Validation
{
    /// <summary>
    /// Validador do request de reset.
    /// </summary>
    public interface IWorldResetValidator
    {
        ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
