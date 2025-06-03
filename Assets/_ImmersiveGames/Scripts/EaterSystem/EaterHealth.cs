using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealth : HealthResource, IResettable
    {
        private EventBinding<EaterConsumptionSatisfiedEvent> _consumptionSatisfiedBinding;

        protected override void Awake()
        {
            base.Awake();
            _consumptionSatisfiedBinding = new EventBinding<EaterConsumptionSatisfiedEvent>(OnConsumptionSatisfied);
        }

        private void OnEnable()
        {
            EventBus<EaterConsumptionSatisfiedEvent>.Register(_consumptionSatisfiedBinding);
        }

        private void OnDisable()
        {
            EventBus<EaterConsumptionSatisfiedEvent>.Unregister(_consumptionSatisfiedBinding);
        }

        private void OnConsumptionSatisfied(EaterConsumptionSatisfiedEvent evt)
        {
            var hunger = GetComponent<EaterHunger>();
            if (hunger && hunger.DesireConfig)
            {
                Heal(hunger.DesireConfig.DesiredHealthRestored);
                DebugUtility.Log<EaterHealth>($"EaterHealth: Restaurado {hunger.DesireConfig.DesiredHealthRestored} HP devido a consumo satisfatório.");
            }
            else
            {
                DebugUtility.LogWarning<EaterHealth>("EaterHunger ou DesireConfig não encontrado para restaurar HP!", this);
            }
        }

        public override void Deafeat(Vector3 position)
        {
            base.Deafeat(position);
            EventBus<EaterDeathEvent>.Raise(new EaterDeathEvent(position, gameObject));
            DebugUtility.Log<EaterHealth>($"EaterHealth: Eater derrotado na posição {position}.");
        }

        public new void Reset()
        {
            base.Reset();
            DebugUtility.Log<EaterHealth>("EaterHealth resetado.");
        }

        public float GetCurrentHealth()
        {
            return currentValue;
        }
    }
}