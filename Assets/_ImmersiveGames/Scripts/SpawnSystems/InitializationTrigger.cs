using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class InitializationTrigger : ISpawnTrigger
    {
        public IPredicate TriggerCondition { get; } = new InitializationPredicate();

        public void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (TriggerCondition.Evaluate())
            {
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            }
        }

        public void Reset() { } // Não precisa de reset
    }
}