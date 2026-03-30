using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.InputModes.Runtime
{
    public interface IPlayerInputLocator
    {
        PlayerInput[] GetActivePlayerInputs();
    }
}
