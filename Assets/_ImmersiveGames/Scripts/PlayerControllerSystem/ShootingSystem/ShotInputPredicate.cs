using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class ShotInputPredicate : IPredicate
    {
        private readonly PlayerInputActions _inputActions;
        private bool _isActive = true;
        private bool _isFiring;

        public ShotInputPredicate(PlayerInputActions inputActions)
        {
            _inputActions = inputActions ?? throw new System.ArgumentNullException(nameof(inputActions));
            _inputActions.Player.Fire.performed += ctx => _isFiring = true;
            _inputActions.Player.Fire.canceled += ctx => _isFiring = false;
        }

        public bool Evaluate()
        {
            return _isActive && _isFiring;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                _isFiring = false;
            }
        }
    }
}