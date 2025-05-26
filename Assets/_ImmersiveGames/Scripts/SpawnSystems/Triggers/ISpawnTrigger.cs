using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public interface ISpawnTrigger
    {
        IPredicate TriggerCondition { get; }
        void CheckTrigger(Vector3 origin, SpawnData data);
        void Reset();
        void SetActive(bool active);
    }
}