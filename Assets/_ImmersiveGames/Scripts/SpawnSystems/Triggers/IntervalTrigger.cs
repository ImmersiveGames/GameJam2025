using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public class IntervalTriggerOld : TimedTriggerOld
    {
        private readonly float _interval;
        private readonly bool _startImmediately;
        private float _timer;

        public IntervalTriggerOld(EnhancedTriggerData data) : base(data)
        {
            _interval = data.GetProperty("interval", 2f);
            if (_interval <= 0f)
            {
                DebugUtility.LogError<IntervalTriggerOld>("Interval deve ser maior que 0. Usando 2s.", spawnPoint);
                _interval = 2f;
            }
            _startImmediately = data.GetProperty("startImmediately", true);
            _timer = _startImmediately ? 0f : _interval;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            DebugUtility.LogVerbose<IntervalTriggerOld>($"Inicializado com interval={_interval}s, startImmediately={_startImmediately} para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _interval;
                DebugUtility.Log<IntervalTriggerOld>($"Trigger disparado para '{spawnPoint.name}' na posição {triggerPosition} a cada {_interval}s.", "green", spawnPoint);
                return true;
            }
            return false;
        }
    }
}