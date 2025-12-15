using ImmersiveGames.RuntimeAttributes.Configs;
using ImmersiveGames.RuntimeAttributes.Services;
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

        public DamageLifecycleModule(string entityId)
        {
            _entityId = entityId;
            IsDead = false;
        }

        public bool IsDead { get; private set; }
        public bool DisableSkinOnDeath { get; set; } = true;

        public bool TriggerGameOverOnDeath { get; set; }

        public void CheckDeath(RuntimeAttributeContext system, RuntimeAttributeType runtimeAttributeType)
        {
            var value = system.Get(runtimeAttributeType);
            if (value == null) return;

            if (value.GetCurrentValue() > 0)
            {
                if (!IsDead) return;
                IsDead = false;
                FilteredEventBus<ReviveEvent>.RaiseFiltered(new ReviveEvent(_entityId), _entityId);
                return;
            }

            if (IsDead) return;
            IsDead = true;
            FilteredEventBus<DeathEvent>.RaiseFiltered(
                new DeathEvent(_entityId, runtimeAttributeType, DisableSkinOnDeath, TriggerGameOverOnDeath),
                _entityId);
        }

        public void RevertDeathState(bool previousState, RuntimeAttributeType runtimeAttributeType)
        {
            if (IsDead == previousState)
            {
                return;
            }

            IsDead = previousState;

            if (IsDead)
            {
                FilteredEventBus<DeathEvent>.RaiseFiltered(
                    new DeathEvent(_entityId, runtimeAttributeType, DisableSkinOnDeath, TriggerGameOverOnDeath),
                    _entityId);
            }
            else
            {
                FilteredEventBus<ReviveEvent>.RaiseFiltered(new ReviveEvent(_entityId), _entityId);
            }
        }

        public void Clear()
        {
            IsDead = false;
            FilteredEventBus<ResetEvent>.RaiseFiltered(new ResetEvent(_entityId), _entityId);
        }
    }
}