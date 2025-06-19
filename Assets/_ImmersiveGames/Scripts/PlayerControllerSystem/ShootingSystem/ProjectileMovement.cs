using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [DebugLevel(DebugLevel.Logs)]
    public class ProjectileMovement : MonoBehaviour,IMoveObject
    {
        private Vector3 _direction;
        private float _speed;
        private bool _isMoving;
        

        private void Update()
        {
            if (!_isMoving) return;
            transform.position += _direction * (_speed * Time.deltaTime);
        }
        public void Initialize(Vector3? direction, float speed, Transform target = null)
        {
            DebugUtility.Log<ProjectileMovement>($"Velocidade inicializada: {speed}, Direção: {direction}", "cyan", this);
            _direction = direction?.normalized ?? Vector3.zero;
            _isMoving = true;
            _speed = speed > 0f ? speed : 10f; // Valor padrão se speed for 0
            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }
        
        public void StopMovement()
        {
            _isMoving = false;
        }
    }
}