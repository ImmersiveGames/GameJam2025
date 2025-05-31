using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class ProjectileMovement : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private bool _isMoving;

        public void InitializeMovement(Vector3 direction, float speed)
        {
            _direction = direction.normalized;
            _isMoving = true;
            _speed = speed;
            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }

        public void StopMovement()
        {
            _isMoving = false;
        }

        private void Update()
        {
            if (!_isMoving) return;
            transform.position += _direction * (_speed * Time.deltaTime);
        }
    }
}