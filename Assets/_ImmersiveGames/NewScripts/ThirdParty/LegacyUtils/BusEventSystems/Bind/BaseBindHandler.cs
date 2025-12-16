using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    public abstract class BaseBindHandler<TEvent, TUI, TData> : MonoBehaviour
        where TEvent : IEvent
        where TUI : Component
    {
        protected readonly Dictionary<string, TUI> bindings = new();
        protected EventBinding<TEvent> eventBinding;

        // ✅ APENAS MÉTODOS ESSENCIAIS - remover duplicatas
        protected virtual void OnEnable() => Register();
        protected virtual void OnDisable() => Unregister();

        protected virtual void Register()
        {
            if (DependencyManager.Provider != null)
                DependencyManager.Provider.InjectDependencies(this);
            
            eventBinding = new EventBinding<TEvent>(HandleEvent);
            EventBus<TEvent>.Register(eventBinding);
        }

        protected virtual void Unregister()
        {
            if (eventBinding != null)
                EventBus<TEvent>.Unregister(eventBinding);
        }

        // ✅ MÉTODO PROTEGIDO para uso interno
        protected void UpdateBinding(string actorId, object resourceType, object data)
        {
            string key = CreateBindingKey(actorId, resourceType);
            if (bindings.TryGetValue(key, out var ui) && ui is IBindableUI<TData> bindableUI && data is TData typedData)
            {
                bindableUI.UpdateValue(typedData);
            }
        }

        protected virtual string CreateBindingKey(string actorId, object resourceType)
            => $"{actorId}_{resourceType}";

        public abstract void HandleEvent(TEvent @event);
        protected abstract TUI CreateUI(TEvent @event);

        protected virtual void OnDestroy() => bindings.Clear();
    }
}