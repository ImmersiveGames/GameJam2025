using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    public interface IEventBinding<T> {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }
    
    public interface IUIFactory<in TEvent, TUI> where TEvent : IEvent
    {
        TUI CreateUI(TEvent evt, Transform parent);
        void ReturnToPool(TUI ui);
    }
    
}