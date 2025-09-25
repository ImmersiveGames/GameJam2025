using System.Collections.Generic;
namespace _ImmersiveGames.Scripts.ResourceSystems
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
    /// <summary>
    /// Interface para gerenciamento de valores de recursos (aumentar, diminuir, obter valores).
    /// </summary>
    public interface IResourceValue
    {
        float GetCurrentValue();
        float GetMaxValue();
        float GetPercentage();
        void Increase(float amount);
        void Decrease(float amount);
        void SetCurrentValue(float value);
    }
    public interface IEntityResourceSystem
    {
        string ActorId { get; }
        void AddResource(ResourceType type, int initialValue, int maxValue);
        void RemoveResource(ResourceType type);
        IResourceValue GetResource(ResourceType type);
        bool HasResource(ResourceType type);
        void ModifyResource(ResourceType type, float delta);
        void SetResourceValue(ResourceType type, float value);
        Dictionary<ResourceType, IResourceValue> GetAllResources();
    }
}