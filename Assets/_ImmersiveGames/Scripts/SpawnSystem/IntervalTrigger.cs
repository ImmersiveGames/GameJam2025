using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
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
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.ObjectName, origin, data));
            }
        }
    }
}