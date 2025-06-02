using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(fileName = "DeathEventPredicate", menuName = "ImmersiveGames/Predicates/DeathEvent Predicate")]
    public class DeathEventPredicateSo : PredicateSo
    {
        private bool _isTriggered;
        private Vector3 _triggerPosition;
        private EventBinding<DeathEvent> _deathEventBinding;

        public Vector3 TriggerPosition => _triggerPosition;

        private void OnEnable()
        {
            _isTriggered = false;
            _triggerPosition = Vector3.zero;
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
            if (_isTriggered) return; // Ignorar eventos adicionais até o reset
            _isTriggered = true;
            _triggerPosition = evt.CustomSpawnPoint ?? evt.Position; 
            Debug.Log($"DeathEventPredicateSo: Recebeu DeathEvent com posição {_triggerPosition} do objeto {evt.Source.name}.");
        }

        public override bool Evaluate()
        {
            bool result = isActive && _isTriggered;
            if (result)
            {
                Debug.Log($"DeathEventPredicateSo: Evaluate retornou true para posição {_triggerPosition}.");
            }
            return result;
        }

        public override void Reset()
        {
            _isTriggered = false;
            _triggerPosition = Vector3.zero;
            Debug.Log("DeathEventPredicateSo: Estado resetado.");
        }
    }
}