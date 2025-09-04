using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public class InitializationTriggerOld : BaseTriggerOld
    {
        private readonly float _delay;
        private bool _hasSpawned;
        private float _timer;

        public InitializationTriggerOld(EnhancedTriggerData data) : base(data)
        {
            _delay = data.GetProperty("delay", 0f);
            if (_delay < 0f)
            {
                DebugUtility.LogError<InitializationTriggerOld>("Delay não pode ser negativo. Usando 0.", spawnPoint);
                _delay = 0f;
            }
            _hasSpawned = false;
            _timer = _delay;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            DebugUtility.LogVerbose<InitializationTriggerOld>($"Inicializado com delay={_delay}s para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        public override bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;
            if (!isActive || _hasSpawned)
                return false;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasSpawned = true;
                DebugUtility.Log<InitializationTriggerOld>($"Spawn inicial disparado para '{spawnPoint.name}' na posição {triggerPosition}.", "green", spawnPoint);
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            _hasSpawned = false;
            _timer = _delay;
            DebugUtility.LogVerbose<InitializationTriggerOld>($"Resetado para '{spawnPoint?.name}' com delay={_delay}s.", "yellow", spawnPoint);
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }
}