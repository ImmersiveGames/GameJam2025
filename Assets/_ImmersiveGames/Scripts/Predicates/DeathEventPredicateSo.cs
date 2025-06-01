using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/DeathEvent Predicate")]
    public class DeathEventPredicateSo : PredicateSo
    {
        private bool isTriggered;
        private Vector3 triggerPosition; // Armazena a posição do evento
        private EventBinding<DeathEvent> _deathEventBinding;

        public Vector3 TriggerPosition => triggerPosition; // Propriedade para acessar a posição

        private void OnEnable()
        {
            isTriggered = false;
            triggerPosition = Vector3.zero;
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            Debug.Log("DeathEventPredicateSo: Registrado no EventBus.");
        }

        private void OnDisable()
        {
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                Debug.Log("DeathEventPredicateSo: Desregistrado do EventBus.");
            }
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            isTriggered = true;
            triggerPosition = evt.Position;
            Debug.Log($"DeathEventPredicateSo: Recebeu DeathEvent na posição {evt.Position} do objeto {evt.Source.name}.");
        }

        public override bool Evaluate()
        {
            bool result = isActive && isTriggered;
            if (result)
            {
                Debug.Log("DeathEventPredicateSo: Evaluate retornou true.");
                Reset();
            }
            return result;
        }

        public override void Reset()
        {
            isTriggered = false;
            triggerPosition = Vector3.zero;
            Debug.Log("DeathEventPredicateSo: Estado resetado.");
        }
    }
}