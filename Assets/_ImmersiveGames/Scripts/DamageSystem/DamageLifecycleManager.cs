using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public static class DamageLifecycleManager
    {
        private static readonly HashSet<string> _deadEntities = new();

        public static void CheckDeath(ResourceSystem system, ResourceType type)
        {
            var value = system.Get(type);
            if (value == null) return;

            string id = system.EntityId;
            if (value.GetCurrentValue() > 0)
            {
                // Se voltou à vida, remover do cache de mortos
                if (_deadEntities.Contains(id))
                {
                    _deadEntities.Remove(id);
                    // Usar FilteredEventBus em vez de EventBus global
                    FilteredEventBus<ReviveEvent>.RaiseFiltered(new ReviveEvent(id), id);
                }
                return;
            }

            // Evitar morte duplicada
            if (!_deadEntities.Add(id))
                return;

            // Usar FilteredEventBus - só o actor morto recebe o evento
            FilteredEventBus<DeathEvent>.RaiseFiltered(new DeathEvent(id, type), id);
        }

        public static void RaiseReset(string entityId)
        {
            _deadEntities.Remove(entityId);
            FilteredEventBus<ResetEvent>.RaiseFiltered(new ResetEvent(entityId), entityId);
        }
    }
}