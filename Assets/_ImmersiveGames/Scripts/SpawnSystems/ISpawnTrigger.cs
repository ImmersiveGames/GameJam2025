using _ImmersiveGames.Scripts.SpawnSystemOLD;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public interface ISpawnTrigger
    {
        IPredicate TriggerCondition { get; }
        void CheckTrigger(Vector3 origin, SpawnData data);
        void Reset(); // Novo
    }
}