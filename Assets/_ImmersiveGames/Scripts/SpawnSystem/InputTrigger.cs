using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class InputTrigger : ISpawnTrigger
    {
        public IPredicate TriggerCondition { get; }

        public InputTrigger(KeyCode key)
        {
            TriggerCondition = new InputPredicate(key);
        }

        public void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (TriggerCondition.Evaluate())
            {
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.ObjectName, origin, data));
            }
        }

        public void Reset() { } // Não precisa de reset
    }
    
}