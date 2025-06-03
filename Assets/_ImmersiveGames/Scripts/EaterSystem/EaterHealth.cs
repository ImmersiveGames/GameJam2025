using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealth : HealthResource, IResettable
    {
        private EventBinding<EaterConsumptionSatisfiedEvent> consumptionSatisfiedBinding;

        protected override void Awake()
        {
            base.Awake();
            consumptionSatisfiedBinding = new EventBinding<EaterConsumptionSatisfiedEvent>(OnConsumptionSatisfied);
        }

        private void OnEnable()
        {
            EventBus<EaterConsumptionSatisfiedEvent>.Register(consumptionSatisfiedBinding);
        }

        private void OnDisable()
        {
            EventBus<EaterConsumptionSatisfiedEvent>.Unregister(consumptionSatisfiedBinding);
        }

        private void OnConsumptionSatisfied(EaterConsumptionSatisfiedEvent evt)
        {
            EaterHunger hunger = GetComponent<EaterHunger>();
            if (hunger != null && hunger.DesireConfig != null)
            {
                Heal(hunger.DesireConfig.DesiredHealthRestored);
                Debug.Log($"EaterHealth: Restaurado {hunger.DesireConfig.DesiredHealthRestored} HP devido a consumo satisfatório.");
            }
            else
            {
                Debug.LogWarning("EaterHunger ou DesireConfig não encontrado para restaurar HP!", this);
            }
        }

        public override void Deafeat(Vector3 position)
        {
            base.Deafeat(position);
            EventBus<EaterDeathEvent>.Raise(new EaterDeathEvent(position, gameObject));
            Debug.Log($"EaterHealth: Eater derrotado na posição {position}.");
        }

        public void Reset()
        {
            base.Reset();
            Debug.Log("EaterHealth resetado.");
        }

        public float GetCurrentHealth()
        {
            return currentValue;
        }
    }
}