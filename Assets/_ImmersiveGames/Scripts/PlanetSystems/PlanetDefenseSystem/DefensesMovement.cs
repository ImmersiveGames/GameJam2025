using System;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    public class DefensesMovement : MonoBehaviour, IMoveObject
    {
        private DefensesMaster _defensesMaster;
        private ProjectilesData _projectilesData;
        private MovementType _movementType;
        private IMovementStrategy _strategy;
        private Vector3 _direction;
        private Transform _target;

        private void Awake()
        {
            if (!TryGetComponent(out _defensesMaster))
            {
                DebugUtility.LogError<DefensesMovement>($"No DefensesMaster found on {gameObject.name}. Please add a DefensesMaster component.");
                return;
            }
            
        }
        public void Initialize(Vector3? direction, float speed, Transform target = null)
        {
            if (_defensesMaster.ProjectilesData is null)
            {
                DebugUtility.LogError<DefensesMovement>($"No ProjectilesData found on {gameObject.name}. Please assign ProjectilesData in the DefensesMaster component.");
                return;
            }
            _projectilesData = _defensesMaster.ProjectilesData; 
            _movementType = _projectilesData.movementType;
            _strategy = CreateStrategy(_movementType);
            _strategy.Initialize(transform, target);
            _direction = direction ?? Vector3.forward;
            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }
        void Update()
        {
            if (_defensesMaster.IsActive)
                _strategy?.Tick();
        }
        private IMovementStrategy CreateStrategy(MovementType tipo)
        {
            switch (tipo)
            {
                case MovementType.Direct:
                    return new DirectMovement(_projectilesData);
                case MovementType.Missile:
                    return new InercialMovement(_projectilesData);
                case MovementType.Spiral:
                    return new SpiralMovement(_projectilesData);
                case MovementType.ZigZag:
                    return new ZigZagMovement(_projectilesData);
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