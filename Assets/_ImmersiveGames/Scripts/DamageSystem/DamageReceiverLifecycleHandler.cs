using System;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Conecta o ResourceSystem de uma entidade ao módulo de ciclo de vida e gera notificações
    /// para o receptor de dano sem depender da origem da alteração do recurso.
    /// </summary>
    internal sealed class DamageReceiverLifecycleHandler : IDisposable
    {
        private readonly ResourceType _resourceType;
        private readonly DamageLifecycleModule _lifecycleModule;
        private readonly DamageExplosionModule _explosionModule;
        private readonly Func<ResourceChangeContext, DamageContext> _contextFactory;
        private readonly Action<DamageLifecycleNotification> _onNotification;

        private ResourceSystem _resourceSystem;
        private DamageContext _pendingContext;
        private bool _hasPendingContext;
        private bool _disposed;

        public DamageReceiverLifecycleHandler(
            ResourceType resourceType,
            DamageLifecycleModule lifecycleModule,
            DamageExplosionModule explosionModule,
            Func<ResourceChangeContext, DamageContext> contextFactory,
            Action<DamageLifecycleNotification> onNotification)
        {
            _resourceType = resourceType;
            _lifecycleModule = lifecycleModule;
            _explosionModule = explosionModule;
            _contextFactory = contextFactory;
            _onNotification = onNotification;
        }

        public bool IsBound => _resourceSystem != null;

        public bool TryAttach(ResourceSystem system)
        {
            if (_disposed || system == null)
            {
                return false;
            }

            if (ReferenceEquals(_resourceSystem, system))
            {
                return true;
            }

            DetachInternal();

            _resourceSystem = system;
            _resourceSystem.ResourceChanged += OnResourceChanged;
            if (_lifecycleModule != null)
            {
                _lifecycleModule.CheckDeath(_resourceSystem, _resourceType);
            }
            return true;
        }

        public void BeginPipeline(DamageContext context)
        {
            if (_disposed)
            {
                return;
            }

            _pendingContext = context;
            _hasPendingContext = context != null;
        }

        public void EndPipeline()
        {
            if (_disposed)
            {
                return;
            }

            _pendingContext = null;
            _hasPendingContext = false;
        }

        private void OnResourceChanged(ResourceChangeContext change)
        {
            if (_disposed || change.ResourceType != _resourceType)
            {
                return;
            }

            var system = change.ResourceSystem ?? _resourceSystem;
            if (system == null || _lifecycleModule == null)
            {
                return;
            }

            bool previousState = _lifecycleModule.IsDead;
            _lifecycleModule.CheckDeath(system, _resourceType);
            bool stateChanged = _lifecycleModule.IsDead != previousState;

            var context = ResolveContext(change, stateChanged);

            if (stateChanged && _lifecycleModule.IsDead && _explosionModule is { HasConfiguration: true })
            {
                _explosionModule.PlayExplosion(context);
            }

            _onNotification?.Invoke(new DamageLifecycleNotification(
                change,
                context,
                stateChanged,
                _lifecycleModule.IsDead));
        }

        private DamageContext ResolveContext(ResourceChangeContext change, bool stateChanged)
        {
            if (_hasPendingContext)
            {
                return _pendingContext;
            }

            if (!change.IsDecrease && !stateChanged)
            {
                return null;
            }

            return _contextFactory?.Invoke(change);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DetachInternal();
            _pendingContext = null;
            _hasPendingContext = false;
        }

        private void DetachInternal()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.ResourceChanged -= OnResourceChanged;
                _resourceSystem = null;
            }
        }
    }
}
