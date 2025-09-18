using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [RequireComponent(typeof(Rigidbody)), DebugLevel(DebugLevel.Error)]
    public class BulletPoolable : PooledObject
    {
        private Rigidbody _rb;
        private BulletObjectData _data;

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            _rb = GetComponent<Rigidbody>();
            _data = config as BulletObjectData;

            if (_rb == null)
                DebugUtility.LogError<BulletPoolable>($"No Rigidbody on {name}", this);
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            if (_rb == null || _data == null) return;

            // Usa a direção fornecida, ou Vector3.zero se não houver direção
            var dir = direction ?? Vector3.zero;
            _rb.linearVelocity = dir.normalized * _data.Speed;
        }

        protected override void OnDeactivated()
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
        }

        protected override void OnReset()
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
        }

        protected override void OnReconfigured(PoolableObjectData config)
        {
            _data = config as BulletObjectData;
        }
    }
}