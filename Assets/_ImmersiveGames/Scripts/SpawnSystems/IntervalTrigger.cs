using _ImmersiveGames.Scripts.SpawnSystemOLD;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class IntervalTrigger : ISpawnTrigger
    {
        public IPredicate TriggerCondition { get; private set; }

        public IntervalTrigger(float interval, bool startImmediately)
        {
            TriggerCondition = new IntervalPredicate(interval, startImmediately);
        }

        public void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (TriggerCondition.Evaluate())
            {
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.ObjectName, origin, data));
            }
        }

        public void Reset()
        {
            if (TriggerCondition is IntervalPredicate predicate)
            {
                predicate.Reset();
            }
        }
    }
}