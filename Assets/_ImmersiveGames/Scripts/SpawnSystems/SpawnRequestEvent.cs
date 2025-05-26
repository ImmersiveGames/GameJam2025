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
}