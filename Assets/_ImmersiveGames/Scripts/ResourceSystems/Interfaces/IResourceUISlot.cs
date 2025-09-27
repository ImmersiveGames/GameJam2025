using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResourceUISlot
    {
        string ExpectedActorId { get; }
        ResourceType ExpectedType { get; }
        bool Matches(IActor actor, ResourceType type);
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
        string EntityId { get; }
        bool IsInitialized { get; }

        // Consulta
        IResourceValue GetResource(ResourceType type);
        bool HasResource(ResourceType type);
        Dictionary<ResourceType, IResourceValue> GetAllResources();

        // Canvas targeting
        string GetTargetCanvasId(ResourceType resourceType);

        // Modificadores
        void ModifyResource(ResourceType type, float delta);
        void SetResourceValue(ResourceType type, float value);
        
        // Novos métodos para instância de recursos
        ResourceInstanceConfig GetResourceInstanceConfig(ResourceType type);
        List<ResourceAutoFlowConfig> GetAutoFlowConfigs();
    }
}