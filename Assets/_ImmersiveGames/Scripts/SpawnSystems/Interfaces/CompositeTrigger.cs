using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class CompositeTrigger : ISpawnTrigger
    {
        private readonly List<ISpawnTrigger> _triggers;
        private readonly CombinationMode _combinationMode;
        private bool _isActive = true;
        private SpawnPoint _spawnPoint;

        public CompositeTrigger(List<ISpawnTrigger> triggers, CombinationMode combinationMode)
        {
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _combinationMode = combinationMode;
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new ArgumentNullException(nameof(spawnPoint));
            foreach (var trigger in _triggers)
            {
                trigger.Initialize(spawnPoint);
            }
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive) return false;

            bool shouldSpawn = _combinationMode == CombinationMode.AND;
            foreach (var trigger in _triggers)
            {
                bool triggered = trigger.CheckTrigger(origin, data);

                if (_combinationMode == CombinationMode.AND && !triggered)
                {
                    shouldSpawn = false;
                    break;
                }
                else if (_combinationMode == CombinationMode.OR && triggered)
                {
                    shouldSpawn = true;
                    break;
                }
            }

            if (shouldSpawn)
            {
                DebugUtility.Log<CompositeTrigger>($"Disparando SpawnRequestEvent com modo {_combinationMode} em {origin}", "green");
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            }

            return shouldSpawn;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            foreach (var trigger in _triggers)
            {
                trigger.SetActive(active);
            }
        }

        public void Reset()
        {
            foreach (var trigger in _triggers)
            {
                trigger.Reset();
            }
        }
    }
}