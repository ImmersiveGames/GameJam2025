using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
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
        string EntityId { get; }
        bool IsInitialized { get; }

        // Consulta
        IResourceValue GetResource(ResourceType type);
        bool HasResource(ResourceType type);
        Dictionary<ResourceType, IResourceValue> GetAllResources();

        // Canvas targeting (necessário para decidir onde criar a UI)
        string GetTargetCanvasId(ResourceType resourceType);

        // Modificadores básicos (opcionais para Orchestrator, mas úteis)
        void ModifyResource(ResourceType type, float delta);
        void SetResourceValue(ResourceType type, float value);
    }
    public interface IDynamicCanvasBinder
    {
        bool CreateSlotsForActor(IActor actor, EntityResourceSystem resourceSystem);
        void RemoveSlotsForActor(IActor actor);
        Transform GetSlotParent();
    }
}