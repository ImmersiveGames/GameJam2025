using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class InputTest : MonoBehaviour
    {
        private PlayerInputActions _inputActions;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Player.Fire.performed += ctx => Debug.Log("Fire pressed!");
            _inputActions.Player.Fire.canceled += ctx => Debug.Log("Fire released!");
        }

        private void OnEnable() => _inputActions.Player.Enable();
        private void OnDisable() => _inputActions.Player.Disable();
    }
}