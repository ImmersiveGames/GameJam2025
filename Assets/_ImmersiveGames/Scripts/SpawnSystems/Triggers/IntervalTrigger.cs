using _ImmersiveGames.Scripts.SpawnSystems.Predicates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public class IntervalTrigger : ISpawnTrigger
    {
        public IPredicate TriggerCondition { get; }

        public IntervalTrigger(float interval, bool startImmediately)
        {
            TriggerCondition = new IntervalPredicate(interval, startImmediately);
        }

        public void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (TriggerCondition.Evaluate())
            {
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            }
        }

        public void Reset()
        {
            if (TriggerCondition is IntervalPredicate predicate)
            {
                predicate.Reset();
            }
        }

        public void SetActive(bool active)
        {
            if (TriggerCondition is IntervalPredicate predicate)
            {
                predicate.SetActive(active);
            }
        }
    }
}