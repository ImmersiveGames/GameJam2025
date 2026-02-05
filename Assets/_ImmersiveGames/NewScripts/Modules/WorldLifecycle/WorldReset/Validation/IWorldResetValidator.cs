using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Validation
{
    /// <summary>
    /// Validador do request de reset.
    /// </summary>
    public interface IWorldResetValidator
    {
        ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
