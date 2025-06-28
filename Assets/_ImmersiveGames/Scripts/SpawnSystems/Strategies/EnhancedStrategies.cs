using _ImmersiveGames.Scripts.ActorSystems;
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
        private readonly bool _randomizePosition;
        private readonly float _positionRadius;

        public SimpleSpawnStrategy(EnhancedStrategyData data)
        {
            _offset = data.GetProperty("offset", Vector3.zero);
            _spawnCount = data.GetProperty("spawnCount", 1);
            if (_spawnCount <= 0)
            {
                DebugUtility.LogError<SimpleSpawnStrategy>("spawnCount deve ser maior que 0. Usando 1.");
                _spawnCount = 1;
            }
            _randomizePosition = data.GetProperty("randomizePosition", false);
            _positionRadius = data.GetProperty("positionRadius", 0f);
            if (_randomizePosition && _positionRadius <= 0f)
            {
                DebugUtility.LogWarning<SimpleSpawnStrategy>("positionRadius deve ser maior que 0 quando randomizePosition é true. Desativando randomização.");
                _randomizePosition = false;
            }
        }

        public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<SimpleSpawnStrategy>("ObjectPool é nulo.");
                return;
            }

            int count = Mathf.Min(_spawnCount, pool.GetAvailableCount());
            if (count == 0)
            {
                DebugUtility.LogWarning<SimpleSpawnStrategy>("Nenhum objeto disponível no pool.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = origin + _offset;
                if (_randomizePosition)
                    spawnPos += Random.insideUnitSphere * _positionRadius;

                var obj = pool.GetObject(spawnPos);
                var owner = sourceObject ? sourceObject.GetComponent<IActor>() : null;
                if (obj != null)
                {
                    obj.Activate(spawnPos, owner);
                    DebugUtility.Log<SimpleSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' spawnado em {spawnPos}.", "green", obj.GetGameObject());
                }
                else
                {
                    DebugUtility.LogWarning<SimpleSpawnStrategy>($"Falha ao obter objeto do pool na iteração {i}.");
                }
            }
        }
    }

    public class DirectionalSpawnStrategy : ISpawnStrategy
    {
        private readonly Vector3 _offset;
        private readonly int _spawnCount;
        private readonly bool _randomizeDirection;
        private readonly float _directionVariation;
        private readonly int _speed;

        public DirectionalSpawnStrategy(EnhancedStrategyData data)
        {
            _offset = data.GetProperty("offset", Vector3.zero);
            _spawnCount = data.GetProperty("spawnCount", 1);
            _speed = data.GetProperty("speed", 10);
            if (_spawnCount <= 0)
            {
                DebugUtility.LogError<DirectionalSpawnStrategy>("spawnCount deve ser maior que 0. Usando 1.");
                _spawnCount = 1;
            }
            _randomizeDirection = data.GetProperty("randomizeDirection", false);
            _directionVariation = data.GetProperty("directionVariation", 0f);
            if (_randomizeDirection && _directionVariation <= 0f)
            {
                DebugUtility.LogWarning<DirectionalSpawnStrategy>("directionVariation deve ser maior que 0 quando randomizeDirection é true. Desativando randomização.");
                _randomizeDirection = false;
            }
        }

        public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
        {
            if (!ValidatePool(pool))
                return;

            int count = DetermineSpawnCount(pool);
            if (count == 0)
                return;

            Vector3 baseDirection = GetBaseDirection(sourceObject);
            var owner = sourceObject ? sourceObject.GetComponent<IActor>() : null;
            Vector3 spawnPos = origin + _offset;

            for (int i = 0; i < count; i++)
            {
                SpawnSingleObject(pool, spawnPos, baseDirection, i, owner);
            }
        }

        private bool ValidatePool(ObjectPool pool)
        {
            if (pool == null)
            {
                DebugUtility.LogError<DirectionalSpawnStrategy>("ObjectPool é nulo.");
                return false;
            }
            return true;
        }

        private int DetermineSpawnCount(ObjectPool pool)
        {
            int count = Mathf.Min(_spawnCount, pool.GetAvailableCount());
            if (count == 0)
            {
                DebugUtility.LogWarning<DirectionalSpawnStrategy>("Nenhum objeto disponível no pool.");
            }
            return count;
        }

        private Vector3 GetBaseDirection(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                DebugUtility.LogWarning<DirectionalSpawnStrategy>("sourceObject é nulo. Objetos não terão direção definida.");
                return Vector3.zero;
            }

            return sourceObject.transform.forward.normalized;
        }

        private Vector3 CalculateDirection(Vector3 baseDirection)
        {
            if (baseDirection == Vector3.zero || !_randomizeDirection)
                return baseDirection;

            return Quaternion.Euler(Random.insideUnitSphere * _directionVariation) * baseDirection;
        }

        private void SpawnSingleObject(ObjectPool pool, Vector3 spawnPos, Vector3 baseDirection, int iteration, IActor owner)
        {
            var obj = pool.GetObject(spawnPos);
            if (obj == null)
            {
                DebugUtility.LogWarning<DirectionalSpawnStrategy>($"Falha ao obter objeto do pool na iteração {iteration}.");
                return;
            }
            obj.Activate(spawnPos, owner);
            SetupObjectMovement(obj, baseDirection, spawnPos);
        }

        private void SetupObjectMovement(IPoolable obj, Vector3 baseDirection, Vector3 spawnPos)
        {
            var movement = obj.GetGameObject().GetComponent<IMoveObject>();
            if (movement == null)
            {
                DebugUtility.LogWarning<DirectionalSpawnStrategy>(
                    $"Objeto '{obj.GetGameObject().name}' não tem IMoveObject. Spawnado sem direção.",
                    obj.GetGameObject());
                return;
            }

            Vector3 finalDirection = CalculateDirection(baseDirection);
            movement.Initialize(finalDirection, _speed); // Velocidade gerenciada externamente

            DebugUtility.Log<DirectionalSpawnStrategy>(
                $"Objeto '{obj.GetGameObject().name}' spawnado em {spawnPos} com direção {finalDirection}.",
                "green",
                obj.GetGameObject());
        }
    }

    public class FullPoolSpawnStrategy : ISpawnStrategy
        {
            private readonly Vector3 _spacingVector;
            private readonly int _maxSpawnCount;

            public FullPoolSpawnStrategy(EnhancedStrategyData data)
            {
                _spacingVector = data.GetProperty("spacingVector", new Vector3(data.GetProperty("spacing", 1f), 0, 0));
                if (_spacingVector == Vector3.zero)
                {
                    DebugUtility.LogError<FullPoolSpawnStrategy>("spacingVector não pode ser zero. Usando (1, 0, 0).");
                    _spacingVector = new Vector3(1f, 0, 0);
                }
                _maxSpawnCount = data.GetProperty("maxSpawnCount", int.MaxValue);
                if (_maxSpawnCount <= 0)
                {
                    DebugUtility.LogError<FullPoolSpawnStrategy>("maxSpawnCount deve ser maior que 0. Usando int.MaxValue.");
                    _maxSpawnCount = int.MaxValue;
                }
            }

            public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
            {
                if (pool == null)
                {
                    DebugUtility.LogError<FullPoolSpawnStrategy>("ObjectPool é nulo.");
                    return;
                }

                int count = Mathf.Min(pool.GetAvailableCount(), _maxSpawnCount);
                if (count == 0)
                {
                    DebugUtility.LogWarning<FullPoolSpawnStrategy>("Nenhum objeto disponível no pool ou maxSpawnCount atingido.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    Vector3 spawnPos = origin + _spacingVector * i;
                    var owner = sourceObject ? sourceObject.GetComponent<IActor>() : null;
                    var obj = pool.GetObject(spawnPos);
                    if (obj != null)
                    {
                        obj.Activate(spawnPos,owner);
                        DebugUtility.Log<FullPoolSpawnStrategy>($"Objeto '{obj.GetGameObject().name}' spawnado em {spawnPos}.", "green", obj.GetGameObject());
                    }
                    else
                    {
                        DebugUtility.LogWarning<FullPoolSpawnStrategy>($"Falha ao obter objeto do pool na iteração {i}.");
                    }
                }
            }
        }
}