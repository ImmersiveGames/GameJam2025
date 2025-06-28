using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class MovementController : MonoBehaviour, IMoveObject
    {
        [Header("Configuração")]
        [SerializeField] private ProjectilesData projectilesData;
        
        private MovementType _movementType;
        private IMovementStrategy _strategy;
        private Vector3 _direction;
        private Transform _target;

        private void Awake()
        {
            _movementType = projectilesData.movementType;
            _strategy = CreateStrategy(_movementType);
        }
        public void Initialize(Vector3? direction, float speed, Transform target = null)
        {
            _strategy.Initialize(transform, target);
            _direction = direction ?? Vector3.forward;
            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }
        void Update()
        {
            _strategy?.Tick();
        }
        private IMovementStrategy CreateStrategy(MovementType tipo)
        {
            switch (tipo)
            {
                case MovementType.Direct:
                    return new DirectMovement(projectilesData);
                case MovementType.Missile:
                    return new InercialMovement(projectilesData);
                case MovementType.Spiral:
                    return new SpiralMovement(projectilesData);
                case MovementType.ZigZag:
                    return new ZigZagMovement(projectilesData);
                default:
                    return null;
            }
        }
        
        public void ChangeStrategy(MovementType newType)
        {
            if (_strategy != null)
            {
                _strategy = CreateStrategy(newType);
                _strategy.Initialize(transform, _target);
            }
        }
        
    }
    public interface IMovementStrategy
    {
        void Initialize(Transform self, Transform target);
        void Tick();
    }
    
    
    public enum MovementType
    {
        None,
        Direct,
        Spiral,
        ZigZag,
        Missile
    }
}