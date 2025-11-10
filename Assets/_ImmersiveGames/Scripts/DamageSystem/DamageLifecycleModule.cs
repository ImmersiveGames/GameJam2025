using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Gerencia o ciclo de vida (morte, revive, reset) de um único ator.
    /// Substitui o DamageLifecycleManager global.
    /// </summary>
    public class DamageLifecycleModule
    {
        private readonly string _entityId;
        private bool _isDead;
        private bool _disableSkinOnDeath = true;
        private bool _triggerGameOverOnDeath;

        public DamageLifecycleModule(string entityId)
        {
            _entityId = entityId;
            _isDead = false;
        }

        public bool IsDead => _isDead;
        public bool DisableSkinOnDeath
        {
            get => _disableSkinOnDeath;
            set => _disableSkinOnDeath = value;
        }

        public bool TriggerGameOverOnDeath
        {
            get => _triggerGameOverOnDeath;
            set => _triggerGameOverOnDeath = value;
        }

        public void CheckDeath(ResourceSystem system, ResourceType resourceType)
        {
            var value = system.Get(resourceType);
            if (value == null) return;

            if (value.GetCurrentValue() > 0)
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
                FilteredEventBus<DeathEvent>.RaiseFiltered(
                    new DeathEvent(_entityId, resourceType, _disableSkinOnDeath, _triggerGameOverOnDeath),
                    _entityId);
            }
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
                FilteredEventBus<DeathEvent>.RaiseFiltered(
                    new DeathEvent(_entityId, resourceType, _disableSkinOnDeath, _triggerGameOverOnDeath),
                    _entityId);
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
    }
}