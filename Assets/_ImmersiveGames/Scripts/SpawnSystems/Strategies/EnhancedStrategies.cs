using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SimpleSpawnStrategy : ISpawnStrategy
    {
        private readonly Vector3 _offset;
        private readonly int _spawnCount;

        public SimpleSpawnStrategy(EnhancedStrategyData data)
        {
            _offset = data.GetProperty("offset", Vector3.zero);
            _spawnCount = data.GetProperty("spawnCount", 1); // Novo: define quantidade
        }

        public void Spawn(ObjectPool pool, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (pool == null) return;

            int count = Mathf.Min(_spawnCount, pool.GetAvailableCount());
            for (int i = 0; i < count; i++)
            {
                var obj = pool.GetObject(origin + _offset);
                if (obj != null)
                {
                    obj.Activate(origin + _offset);
                    DebugUtility.Log<SimpleSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' spawnado em {origin + _offset}.", "green", obj.GetGameObject());
                }
            }
        }
    }

    public class DirectionalSpawnStrategy : ISpawnStrategy
    {
        private readonly float _speed;
        private readonly Vector3 _offset;
        private readonly int _spawnCount;

        public DirectionalSpawnStrategy(EnhancedStrategyData data)
        {
            _speed = data.GetProperty("speed", 5f);
            _offset = data.GetProperty("offset", Vector3.zero);
            _spawnCount = data.GetProperty("spawnCount", 1);
        }

        public void Spawn(ObjectPool pool, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (pool == null) return;

            int count = Mathf.Min(_spawnCount, pool.GetAvailableCount());
            for (int i = 0; i < count; i++)
            {
                var obj = pool.GetObject(origin + _offset);
                if (obj != null)
                {
                    obj.Activate(origin + _offset);
                    var movement = obj.GetGameObject().GetComponent<IObjectMovement>();
                    if (movement != null)
                    {
                        movement.Initialize(forward.normalized, _speed);
                        DebugUtility.Log<DirectionalSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' spawnado em {origin + _offset} com direção {forward}.", "green", obj.GetGameObject());
                    }
                    else
                    {
                        DebugUtility.LogError<DirectionalSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' não tem IObjectMovement.", obj.GetGameObject());
                    }
                }
            }
        }
    }

    public class FullPoolSpawnStrategy : ISpawnStrategy
    {
        private readonly float _spacing;

        public FullPoolSpawnStrategy(EnhancedStrategyData data)
        {
            _spacing = data.GetProperty("spacing", 1f);
        }

        public void Spawn(ObjectPool pool, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (pool == null) return;

            int count = pool.GetAvailableCount();
            for (int i = 0; i < count; i++)
            {
                var obj = pool.GetObject(origin + new Vector3(i * _spacing, 0, 0));
                if (obj != null)
                {
                    obj.Activate(origin + new Vector3(i * _spacing, 0, 0));
                    DebugUtility.Log<FullPoolSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' spawnado em {origin + new Vector3(i * _spacing, 0, 0)}.", "green", obj.GetGameObject());
                }
            }
        }
    }
}