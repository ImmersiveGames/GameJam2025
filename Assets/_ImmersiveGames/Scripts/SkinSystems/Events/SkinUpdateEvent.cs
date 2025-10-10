using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Evento global para notificar mudanças de skin individual
    /// </summary>
    public struct SkinUpdateEvent : IEvent
    {
        public ISkinConfig SkinConfig { get; }
        public IActor Actor { get; }

        public SkinUpdateEvent(ISkinConfig skinConfig, IActor actor)
        {
            SkinConfig = skinConfig;
            Actor = actor;
        }
    }

    /// <summary>
    /// Evento global para notificar mudanças de coleção de skins
    /// </summary>
    public struct SkinCollectionUpdateEvent : IEvent
    {
        public SkinCollectionData SkinCollection { get; }
        public IActor Actor { get; }

        public SkinCollectionUpdateEvent(SkinCollectionData skinCollection, IActor actor)
        {
            SkinCollection = skinCollection;
            Actor = actor;
        }
    }

    /// <summary>
    /// Evento global para notificar criação de instâncias de skin
    /// </summary>
    public struct SkinInstancesCreatedEvent : IEvent
    {
        public ModelType ModelType { get; }
        public UnityEngine.GameObject[] Instances { get; }
        public IActor Actor { get; }

        public SkinInstancesCreatedEvent(ModelType modelType, UnityEngine.GameObject[] instances, IActor actor)
        {
            ModelType = modelType;
            Instances = instances;
            Actor = actor;
        }
    }
}