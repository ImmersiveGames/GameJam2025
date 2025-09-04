using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public class PredicateTriggerOld : TimedTriggerOld
    {
        private readonly float _checkInterval;
        private System.Func<SpawnPoint, bool> _predicate;
        private float _timer;

        public PredicateTriggerOld(EnhancedTriggerData data) : base(data)
        {
            _checkInterval = data.GetProperty("checkInterval", 0.5f);
            if (_checkInterval <= 0f)
            {
                DebugUtility.LogError<PredicateTriggerOld>("checkInterval deve ser maior que 0. Usando 0.5s.", spawnPoint);
                _checkInterval = 0.5f;
            }
            _timer = _checkInterval;
            _predicate = (_) => false;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            if (_predicate == null || !_predicate(spawnPoint))
            {
                DebugUtility.LogWarning<PredicateTriggerOld>("Predicado não configurado. Trigger não disparará até SetPredicate ser chamado.", spawnPoint);
            }
            DebugUtility.LogVerbose<PredicateTriggerOld>($"Inicializado com checkInterval={_checkInterval}s para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _checkInterval;
                if (_predicate(spawnPoint))
                {
                    DebugUtility.LogVerbose<PredicateTriggerOld>($"Spawn disparado por predicado em '{spawnPoint.name}' na posição {triggerPosition}.", "green", spawnPoint);
                    return true;
                }
            }
            return false;
        }

        public void SetPredicate(System.Func<SpawnPoint, bool> predicate)
        {
            _predicate = predicate ?? (_ => false);
            DebugUtility.LogVerbose<PredicateTriggerOld>($"Predicado configurado para '{spawnPoint?.name}'.", "blue", spawnPoint);
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}