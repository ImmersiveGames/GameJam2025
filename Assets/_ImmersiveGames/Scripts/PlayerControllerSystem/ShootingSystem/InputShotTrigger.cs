using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class InputShotTrigger : ISpawnTrigger
    {
        public IPredicate TriggerCondition { get; }
        private float _fireRate;
        private float _nextFireTime;
        private bool _isActive = true;

        public InputShotTrigger(PlayerInputActions inputActions)
        {
            TriggerCondition = new ShotInputPredicate(inputActions);
        }

        public void SetFireRate(float fireRate)
        {
            _fireRate = fireRate > 0 ? fireRate : 1f;
            if (_nextFireTime == 0)
            {
                _nextFireTime = Time.time;
            }
        }

        public void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || !TriggerCondition.Evaluate() || Time.time < _nextFireTime)
                return;

            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            _nextFireTime = Time.time + (1f / _fireRate);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            TriggerCondition.SetActive(active);
            if (!active)
            {
                _nextFireTime = 0;
            }
        }

        public void Reset()
        {
            _nextFireTime = 0;
        }
    }
}