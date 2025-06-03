using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(fileName = "DeathEventPredicate", menuName = "ImmersiveGames/Predicates/DeathEvent Predicate")]
    [DebugLevel(DebugLevel.Warning)]
    public class DeathEventPredicateSo : PredicateSo
    {
        private bool _isTriggered;
        private EventBinding<DeathEvent> _deathEventBinding;

        public Vector3 TriggerPosition { get; private set; }

        private void OnEnable()
        {
            _isTriggered = false;
            TriggerPosition = Vector3.zero;
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.Log<DeathEventPredicateSo>("DeathEventPredicateSo: Registrado no EventBus.");
        }

        private void OnDisable()
        {
            if (_deathEventBinding == null) return;
            EventBus<DeathEvent>.Unregister(_deathEventBinding);
            DebugUtility.Log<DeathEventPredicateSo>("DeathEventPredicateSo: Desregistrado do EventBus.");
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (_isTriggered) return; // Ignorar eventos adicionais até o reset
            _isTriggered = true;
            TriggerPosition = evt.CustomSpawnPoint ?? evt.Position; 
            DebugUtility.Log<DeathEventPredicateSo>($"DeathEventPredicateSo: Recebeu DeathEvent com posição {TriggerPosition} do objeto {evt.Source.name}.");
        }

        public override bool Evaluate()
        {
            bool result = isActive && _isTriggered;
            if (result)
            {
                DebugUtility.Log<DeathEventPredicateSo>($"DeathEventPredicateSo: Evaluate retornou true para posição {TriggerPosition}.");
            }
            return result;
        }

        public override void Reset()
        {
            _isTriggered = false;
            TriggerPosition = Vector3.zero;
            DebugUtility.Log<DeathEventPredicateSo>("DeathEventPredicateSo: Estado resetado.");
        }
    }
}