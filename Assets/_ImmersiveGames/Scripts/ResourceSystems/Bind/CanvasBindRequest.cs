using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public struct CanvasBindRequest : IEvent
    {
        public readonly string key;
        public readonly string actorId;
        public readonly ResourceType resourceType;
        public readonly IResourceValue data;
        public readonly string targetCanvasId;
        public readonly float requestTime;
    
        public CanvasBindRequest(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            key = $"{actorId}_{resourceType}";
            this.actorId = actorId;
            this.resourceType = resourceType;
            this.data = data;
            this.targetCanvasId = targetCanvasId;
            requestTime = Time.time;
        }
    }
}