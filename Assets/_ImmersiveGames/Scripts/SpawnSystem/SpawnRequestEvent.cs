using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public struct SpawnRequestEvent : IEvent
    {
        public string ObjectName { get; }
        public Vector3 Origin { get; }
        public SpawnData Data { get; }

        public SpawnRequestEvent(string objectName, Vector3 origin, SpawnData data)
        {
            ObjectName = objectName;
            Origin = origin;
            Data = data;
        }
    }

    public struct ObjectSpawnedEvent : IEvent
    {
        public IPoolable Object { get; }
        public Vector3 Position { get; }

        public ObjectSpawnedEvent(IPoolable obj, Vector3 position)
        {
            Object = obj;
            Position = position;
        }
    }
    public class PoolExhaustedEvent : IEvent
    {
        public string PoolKey { get; }

        public PoolExhaustedEvent(string poolKey)
        {
            PoolKey = poolKey;
        }
    }
}