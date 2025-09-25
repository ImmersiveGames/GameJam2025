using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
namespace _ImmersiveGames.Scripts.NewResourceSystem.UI
{
    public interface IResourceUISlot
    {
        string SlotId { get; }
        string ExpectedActorId { get; }
        ResourceType ExpectedType { get; }
        void Configure(IResourceValue data);
        void Clear();
        void SetVisible(bool visible);
    }

    public interface ICanvasResourceBinder
    {
        string CanvasId { get; }
        bool TryBindActor(string actorId, ResourceType type, IResourceValue data);
        void UnbindActor(string actorId);
        void UpdateResource(string actorId, ResourceType type, IResourceValue data);
    }
}