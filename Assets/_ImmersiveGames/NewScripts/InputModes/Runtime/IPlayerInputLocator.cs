using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    public interface IPlayerInputLocator
    {
        PlayerInput[] GetActivePlayerInputs();
    }
}

