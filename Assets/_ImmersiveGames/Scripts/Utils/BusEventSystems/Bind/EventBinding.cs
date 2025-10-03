﻿using System;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    public class EventBinding<T> : IEventBinding<T> where T : IEvent {
        private Action<T> _onEvent = _ => { };
        private Action _onEventNoArgs = () => { };

        //TODO: verificar porque esse é subutilizado
        public Action<T> OnEvent {
            get => _onEvent;
            set => _onEvent = value;
        }

        public Action OnEventNoArgs {
            get => _onEventNoArgs;
            set => _onEventNoArgs = value;
        }

        public EventBinding(Action<T> onEvent)=>_onEvent = onEvent;
        public EventBinding(Action onEventNoArgs) => _onEventNoArgs = onEventNoArgs;
        
        public void Add(Action<T> onEvent)=>_onEvent += onEvent;
        public void Add(Action onEventNoArgs) => _onEventNoArgs += onEventNoArgs;
        public void Remove(Action<T> onEvent)=>_onEvent -= onEvent;
        public void Remove(Action onEventNoArgs) => _onEventNoArgs = onEventNoArgs;
    }
}