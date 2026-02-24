using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
namespace _ImmersiveGames.Scripts.SkinSystems.Events
{
    /// <summary>
    /// Evento global para notificar mudan�as de skin individual
    /// </summary>
    public struct SkinEvents : IEvent
    {
        public ISkinConfig SkinConfig { get; }
        public IActor Actor { get; }

        public SkinEvents(ISkinConfig skinConfig, IActor actor)
        {
            SkinConfig = skinConfig;
            Actor = actor;
        }
    }

    /// <summary>
    /// Evento global para notificar mudan�as de cole��o de skins
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
    /// Evento global para notificar cria��o de inst�ncias de skin
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
