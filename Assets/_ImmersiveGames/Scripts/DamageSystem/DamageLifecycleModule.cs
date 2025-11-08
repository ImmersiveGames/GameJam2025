using System;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Gerencia o ciclo de vida (morte, revive, reset) de um único ator.
    /// Substitui o DamageLifecycleManager global.
    /// </summary>
    public class DamageLifecycleModule : IDisposable
    {
        private readonly string _entityId;
        private readonly EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private bool _isDead;
        private ResourceSystem _resourceSystem;
        private ResourceType? _watchedResource;

        public DamageLifecycleModule(string entityId)
        {
            _entityId = entityId;
            _isDead = false;

            _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdateEvent);
            EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
        }

        public bool IsDead => _isDead;

        /// <summary>
        /// Vincula o módulo ao <see cref="ResourceSystem"/> alvo para acompanhar todas as mudanças
        /// de recurso relevantes. Dessa forma, mudanças externas (debug, autoflow, etc.) também
        /// disparam os eventos de morte/revive.
        /// </summary>
        public void SetWatchedResource(ResourceType resourceType)
        {
            _watchedResource = resourceType;
        }

        public void BindToResourceSystem(ResourceSystem system, ResourceType resourceType)
        {
            _watchedResource = resourceType;

            if (system == null)
            {
                return;
            }

            if (_resourceSystem == system && _watchedResource == resourceType)
            {
                return;
            }

            UnbindInternal();

            _resourceSystem = system;
            _resourceSystem.ResourceChanged += HandleResourceChanged;

            // Garantimos que o estado reflita imediatamente o valor atual.
            CheckDeath(_resourceSystem, resourceType);
        }

        public void CheckDeath(ResourceSystem system, ResourceType resourceType)
        {
            var value = system.Get(resourceType);
            if (value == null) return;

            EvaluateResourceValue(value.GetCurrentValue(), resourceType);
        }

        public void RevertDeathState(bool previousState, ResourceType resourceType)
        {
            if (_isDead == previousState)
            {
                return;
            }

            _isDead = previousState;

            if (_isDead)
            {
                FilteredEventBus<DeathEvent>.RaiseFiltered(new DeathEvent(_entityId, resourceType), _entityId);
            }
            else
            {
                FilteredEventBus<ReviveEvent>.RaiseFiltered(new ReviveEvent(_entityId), _entityId);
            }
        }

        public void Clear()
        {
            _isDead = false;
            FilteredEventBus<ResetEvent>.RaiseFiltered(new ResetEvent(_entityId), _entityId);
        }

        public void Dispose()
        {
            UnbindInternal();

            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            }

            _watchedResource = null;
        }

        private void HandleResourceChanged(ResourceChangeContext context)
        {
            if (context.ResourceSystem == null || !_watchedResource.HasValue)
            {
                return;
            }

            if (context.ResourceType != _watchedResource.Value)
            {
                return;
            }

            if (context.ResourceSystem != _resourceSystem)
            {
                return;
            }

            // Atualizamos o estado toda vez que o recurso monitorado mudar.
            CheckDeath(context.ResourceSystem, context.ResourceType);
        }

        private void OnResourceUpdateEvent(ResourceUpdateEvent evt)
        {
            if (evt == null || !_watchedResource.HasValue)
            {
                return;
            }

            if (!string.Equals(evt.ActorId, _entityId))
            {
                return;
            }

            if (evt.ResourceType != _watchedResource.Value)
            {
                return;
            }

            float currentValue = evt.NewValue?.GetCurrentValue() ?? 0f;
            EvaluateResourceValue(currentValue, evt.ResourceType);
        }

        private void EvaluateResourceValue(float currentValue, ResourceType resourceType)
        {
            if (currentValue > 0f)
            {
                if (_isDead)
                {
                    _isDead = false;
                    FilteredEventBus<ReviveEvent>.RaiseFiltered(new ReviveEvent(_entityId), _entityId);
                }

                return;
            }

            if (!_isDead)
            {
                _isDead = true;
                FilteredEventBus<DeathEvent>.RaiseFiltered(new DeathEvent(_entityId, resourceType), _entityId);
            }
        }

        private void UnbindInternal()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.ResourceChanged -= HandleResourceChanged;
                _resourceSystem = null;
            }
        }
    }
}
