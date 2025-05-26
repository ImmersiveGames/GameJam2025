using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
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