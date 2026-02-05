using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Validation
{
    /// <summary>
    /// Validador do request de reset.
    /// </summary>
    public interface IWorldResetValidator
    {
        ResetDecision Validate(WorldResetRequest request, IWorldResetPolicy policy);
    }
}
