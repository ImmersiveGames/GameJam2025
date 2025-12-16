using System;
using System.Collections.Generic;
using _ImmersiveGames.NewProject.Infrastructure.Logging;

namespace _ImmersiveGames.NewProject.Infrastructure.Events
{
    /// <summary>
    /// EventBus simples e tipado, com filtro opcional por escopo textual.
    /// Não controla fluxo de jogo; apenas notifica listeners.
    /// </summary>
    public sealed class TypedEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
        private readonly ILogger _logger;

        public TypedEventBus(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, string scope = null)
        {
            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var handlers))
            {
                handlers = new List<Subscription>();
                _subscriptions[type] = handlers;
            }

            var subscription = new Subscription(scope, evt => handler((TEvent)evt), () => Remove(type, subscription: null));
            handlers.Add(subscription);

            // Atualiza o callback de remoção agora que temos a referência real.
            subscription.Bind(() => Remove(type, subscription));
            return subscription;
        }

        public void Publish<TEvent>(TEvent payload, string scope = null)
        {
            var type = typeof(TEvent);
            if (!_subscriptions.TryGetValue(type, out var handlers))
            {
                return;
            }

            foreach (var subscription in handlers.ToArray())
            {
                if (!subscription.IsActive)
                {
                    continue;
                }

                if (subscription.Scope == null || subscription.Scope == scope)
                {
                    try
                    {
                        subscription.Handler(payload);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Log(LogLevel.Error, $"Erro ao processar evento {type.Name}", ex);
                    }
                }
            }
        }

        private void Remove(Type type, Subscription subscription)
        {
            if (!_subscriptions.TryGetValue(type, out var handlers))
            {
                return;
            }

            handlers.RemoveAll(sub => !sub.IsActive || sub == subscription);
        }

        private sealed class Subscription : IDisposable
        {
            public string Scope { get; }
            public Delegate Handler { get; }
            public bool IsActive { get; private set; } = true;
            private Action _onDispose;

            public Subscription(string scope, Delegate handler, Action onDispose)
            {
                Scope = scope;
                Handler = handler;
                _onDispose = onDispose;
            }

            public void Bind(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (!IsActive)
                {
                    return;
                }

                IsActive = false;
                _onDispose?.Invoke();
            }
        }
    }
}
