using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Core
{
    /// <summary>
    /// Helper local do módulo para registrar/unregister bindings de serviços/bridges IDisposable.
    /// Não deve ser usado em MonoBehaviours.
    /// </summary>
    public sealed class GameLoopEventSubscriptionSet : IDisposable
    {
        private readonly List<Action> _unregisterActions = new();
        private bool _disposed;

        public void Register<TEvent>(EventBinding<TEvent> binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            EventBus<TEvent>.Register(binding);
            _unregisterActions.Add(() => EventBus<TEvent>.Unregister(binding));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (int i = _unregisterActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _unregisterActions[i]?.Invoke();
                }
                catch
                {
                    /* best-effort */
                }
            }

            _unregisterActions.Clear();
        }
    }
}
