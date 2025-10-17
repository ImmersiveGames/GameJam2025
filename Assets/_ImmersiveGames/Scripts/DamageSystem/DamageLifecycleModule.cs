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

        public DamageLifecycleModule(string entityId)
        {
            _entityId = entityId;
            _isDead = false;
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
                FilteredEventBus<DeathEvent>.RaiseFiltered(new DeathEvent(_entityId, resourceType), _entityId);
            }
        }

        public void Clear()
        {
            _isDead = false;
            FilteredEventBus<ResetEvent>.RaiseFiltered(new ResetEvent(_entityId), _entityId);
        }
    }
}