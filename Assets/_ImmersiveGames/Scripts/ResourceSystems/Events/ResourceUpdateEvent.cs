using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceUpdateEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public IResourceValue NewValue { get; }

        public ResourceUpdateEvent(string actorId, ResourceType resourceType, IResourceValue newValue)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            NewValue = newValue;
        }
    }
    public class CanvasBinderRegisteredEvent : IEvent
    {
        public ICanvasResourceBinder Binder { get; }
        public CanvasBinderRegisteredEvent(ICanvasResourceBinder binder) => Binder = binder;
    }
}