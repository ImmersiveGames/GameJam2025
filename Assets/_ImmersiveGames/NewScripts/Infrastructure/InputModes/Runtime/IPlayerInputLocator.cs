using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime
{
    public interface IPlayerInputLocator
    {
        PlayerInput[] GetActivePlayerInputs();
    }
}
