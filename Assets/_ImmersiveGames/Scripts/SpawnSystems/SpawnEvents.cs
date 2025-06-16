using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
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
    public struct SpawnTriggeredEvent : IEvent
    {
        public string PoolKey { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnTriggeredEvent(string poolKey, Vector3 position, SpawnData data)
        {
            PoolKey = poolKey;
            Position = position;
            Data = data;
        }
    }

    public struct SpawnFailedEvent : IEvent
    {
        public string PoolKey { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnFailedEvent(string poolKey, Vector3 position, SpawnData data)
        {
            PoolKey = poolKey;
            Position = position;
            Data = data;
        }
    }
}