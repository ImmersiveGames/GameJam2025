using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public struct SpawnRequestEvent : IEvent
    {
        public string ObjectName { get; }
        public Vector3 Origin { get; }
        public SpawnData Data { get; }
        public GameObject SourceGameObject { get; } // Novo campo

        public SpawnRequestEvent(string objectName, Vector3 origin, SpawnData data, GameObject sourceGameObject = null)
        {
            ObjectName = objectName;
            Origin = origin;
            Data = data;
            SourceGameObject = sourceGameObject;
        }
    }

    public struct SpawnTriggeredEvent : IEvent
    {
        public string PoolName { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnTriggeredEvent(string poolName, Vector3 position, SpawnData data)
        {
            PoolName = poolName;
            Position = position;
            Data = data;
        }
    }

    public struct SpawnFailedEvent : IEvent
    {
        public string PoolName { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnFailedEvent(string poolName, Vector3 position, SpawnData data)
        {
            PoolName = poolName;
            Position = position;
            Data = data;
        }
    }
}