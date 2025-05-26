using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PooledObjectMover : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private bool _isMoving;

        public void StartMovement(Vector3 direction, float speed)
        {
            _direction = direction.normalized;
            _speed = speed;
            _isMoving = true;
            // Alinha a rotação do projétil com a direção (eixo Z forward)
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