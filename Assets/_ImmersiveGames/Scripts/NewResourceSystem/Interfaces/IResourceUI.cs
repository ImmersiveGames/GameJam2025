using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.NewResourceSystem.Interfaces
{
    public interface IResourceUI : IBindableUI<IResourceValue>
    {
        // Apenas o específico de recursos
        void SetResourceType(ResourceType type);
        void SetVisible(bool visible);
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

    // ✅ Interface segregada para gerenciamento de atores (SRP e ISP)
    public interface IActorRegistry
    {
        void RegisterActor(string actorId);
        void UnregisterActor(string actorId);
        bool IsActorRegistered(string actorId);
        void RemoveActorBindings(string actorId);
        int GetResourceCountForActor(string actorId);
    }

    // ✅ Interface segregada para atualizações de recursos (SRP e ISP)
    public interface IResourceUpdater
    {
        void UpdateResource(string actorId, ResourceType resourceType, IResourceValue newValue);
        void UpdateActorResources(string actorId, Dictionary<ResourceType, IResourceValue> resources);
        IResourceUI GetResourceUI(string actorId, ResourceType resourceType);
        List<IResourceUI> GetActorUIs(string actorId);
        void SetActorUIVisible(string actorId, bool visible);
        bool HasBinding(string actorId, ResourceType resourceType);
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
}