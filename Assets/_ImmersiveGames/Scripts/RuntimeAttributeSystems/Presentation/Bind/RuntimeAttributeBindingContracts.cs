using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.UI;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind
{
    /// <summary>
    /// Contrato único para binders de canvas de atributos (UI ↔ RuntimeAttribute).
    /// Centraliza a identificação, ciclo de injeção e operações de bind.
    /// </summary>
    public interface IAttributeCanvasBinder : IInjectableComponent
    {
        string CanvasId { get; }
        AttributeCanvasType Type { get; }
        AttributeCanvasInitializationState State { get; }

        void ScheduleBind(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data);
        bool CanAcceptBinds();
        IReadOnlyDictionary<string, Dictionary<RuntimeAttributeType, RuntimeAttributeUISlot>> GetActorSlots();
    }

    /// <summary>
    /// Contrato base para qualquer componente que precise participar do ciclo de injeção
    /// controlado pelo <see cref="RuntimeAttributeBootstrapper"/>.
    /// </summary>
    public interface IInjectableComponent
    {
        string GetObjectId();
        void OnDependenciesInjected();
        DependencyInjectionState InjectionState { get; set; }
    }

    /// <summary>
    /// Contrato para bridges que fazem a ponte entre services e componentes de apresentação.
    /// </summary>
    public interface IRuntimeAttributeBridge
    {
        IActor Actor { get; }
        bool IsInitialized { get; }
        bool IsDestroyed { get; }
        RuntimeAttributeContext GetResourceSystem();
    }

    public enum DependencyInjectionState
    {
        Pending,
        Injecting,
        Ready,
        Failed
    }

    public enum AttributeCanvasTargetMode
    {
        Default, // "MainUI"
        ActorSpecific, // "{actorId}_Canvas"
        Custom // customCanvasId
    }

    public enum AttributeCanvasType { Scene, Dynamic }
    public enum AttributeCanvasInitializationState { Pending, Injecting, Ready, Failed }
}
