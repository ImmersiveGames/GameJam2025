using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FXSystems
{
    public class FxSpawnPoint : SpawnPoint
    {
        [SerializeField] private bool useSourcePosition = true; // Usar posição do objeto destruído?
        private EventBinding<DeathEvent> _deathEventBinding;

        protected override void OnEnable()
        {
            base.OnEnable();
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            Debug.Log($"FxSpawnPoint {name}: Registrado para DeathEvent.");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                Debug.Log($"FxSpawnPoint {name}: Desregistrado de DeathEvent.");
            }
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            // Verificar se o evento é relevante para este SpawnPoint
            if (evt.Source == gameObject || IsChildOrSelf(evt.Source))
            {
                if (spawnData != null && spawnData.TriggerStrategy is PredicateTriggerSo predicateTrigger &&
                    predicateTrigger.predicate is DeathEventPredicateSo deathPredicate)
                {
                    Vector3 origin = useSourcePosition ? deathPredicate.TriggerPosition : transform.position;
                    Debug.Log($"FxSpawnPoint {name}: Processando DeathEvent para {evt.Source.name} na posição {origin}.");
                    spawnData.TriggerStrategy.CheckTrigger(origin, spawnData);
                }
                else
                {
                    Debug.LogWarning($"FxSpawnPoint {name}: TriggerStrategy ou Predicate não configurado corretamente.");
                }
            }
        }

        private bool IsChildOrSelf(GameObject source)
        {
            Transform sourceTransform = source.transform;
            bool isRelevant = sourceTransform == transform || sourceTransform.IsChildOf(transform);
            Debug.Log($"FxSpawnPoint {name}: Verificado IsChildOrSelf para {source.name}: {isRelevant}");
            return isRelevant;
        }
    }
}