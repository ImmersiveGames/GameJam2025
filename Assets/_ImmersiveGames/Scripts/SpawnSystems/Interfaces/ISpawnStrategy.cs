using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public abstract class SpawnStrategyConfigBase : ScriptableObject, ISpawnStrategyConfig
    {
        public abstract ISpawnStrategyInstance CreateInstance();
    }

    public interface ISpawnStrategyConfig
    {
        ISpawnStrategyInstance CreateInstance();
    }
    public interface ISpawnStrategyInstance
    {
        string StrategyName { get; }
        IEnumerable<(Vector3 position, Quaternion rotation)> GetSpawnPositions(int count);
        float GetSpawnDelay(int index);
        void SetTarget(Transform target); // Para configurar alvo dinâmico
        void SetPosition(Vector3 position); // Para configurar posição estática
    }
    public interface ISpawnStrategy
    {
        void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null);
    }
    public interface ISpawnTriggerOld
    {
        void Initialize(SpawnPoint spawnPointRef);
        bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);
        void SetActive(bool active);
        void Reset();
        bool IsActive { get; }

        void OnDisable();
    }
    public interface IMoveObject
    { 
        void Initialize(Vector3? direction, float speed, Transform target = null);
    }
    public interface ISizeMeasurable
    {
        float GetDiameter();
    }
}