using System;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Conecta o RuntimeAttributeContext de uma entidade ao módulo de ciclo de vida e gera notificações
    /// para o receptor de dano sem depender da origem da alteração do recurso.
    /// </summary>
    internal sealed class DamageReceiverLifecycleHandler : IDisposable
    {
        private readonly RuntimeAttributeType _runtimeAttributeType;
        private readonly DamageLifecycleModule _lifecycleModule;
        private readonly DamageExplosionModule _explosionModule;
        private readonly Func<RuntimeAttributeChangeContext, DamageContext> _contextFactory;
        private readonly Action<DamageLifecycleNotification> _onNotification;

        private RuntimeAttributeContext _runtimeAttributeContext;
        private DamageContext _pendingContext;
        private bool _hasPendingContext;
        private bool _disposed;

        public DamageReceiverLifecycleHandler(
            RuntimeAttributeType runtimeAttributeType,
            DamageLifecycleModule lifecycleModule,
            DamageExplosionModule explosionModule,
            Func<RuntimeAttributeChangeContext, DamageContext> contextFactory,
            Action<DamageLifecycleNotification> onNotification)
        {
            _runtimeAttributeType = runtimeAttributeType;
            _lifecycleModule = lifecycleModule;
            _explosionModule = explosionModule;
            _contextFactory = contextFactory;
            _onNotification = onNotification;
        }

        public bool IsBound => _runtimeAttributeContext != null;

        public bool TryAttach(RuntimeAttributeContext system)
        {
            if (_disposed || system == null)
            {
                return false;
            }

            if (ReferenceEquals(_runtimeAttributeContext, system))
            {
                return true;
            }

            DetachInternal();

            _runtimeAttributeContext = system;
            _runtimeAttributeContext.ResourceChanged += OnResourceChanged;
            if (_lifecycleModule != null)
            {
                _lifecycleModule.CheckDeath(_runtimeAttributeContext, _runtimeAttributeType);
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

        private void OnResourceChanged(RuntimeAttributeChangeContext change)
        {
            if (_disposed || change.RuntimeAttributeType != _runtimeAttributeType)
            {
                return;
            }

            var system = change.RuntimeAttributeContext ?? _runtimeAttributeContext;
            if (system == null || _lifecycleModule == null)
            {
                return;
            }

            bool previousState = _lifecycleModule.IsDead;
            _lifecycleModule.CheckDeath(system, _runtimeAttributeType);
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

        private DamageContext ResolveContext(RuntimeAttributeChangeContext change, bool stateChanged)
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
            if (_runtimeAttributeContext != null)
            {
                _runtimeAttributeContext.ResourceChanged -= OnResourceChanged;
                _runtimeAttributeContext = null;
            }
        }
    }
}
