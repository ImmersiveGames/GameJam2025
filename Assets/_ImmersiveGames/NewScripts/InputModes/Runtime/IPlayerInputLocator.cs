using UnityEngine.InputSystem;
namespace ImmersiveGames.GameJam2025.Infrastructure.InputModes.Runtime
{
    public interface IPlayerInputLocator
    {
        PlayerInput[] GetActivePlayerInputs();
    }
}

