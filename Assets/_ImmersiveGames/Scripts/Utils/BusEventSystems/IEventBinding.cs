using System;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    public interface IEventBinding<T> {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }
}