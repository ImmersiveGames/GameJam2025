using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/Trigger/Predicate Trigger")]
    [DebugLevel(DebugLevel.Warning)]
    public class PredicateTriggerSo : SpawnTriggerSo
    {
        [SerializeReference] public PredicateSo predicate;

        public override void CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!predicate || !predicate.Evaluate()) return;
            DebugUtility.Log<PredicateTriggerSo>($"PredicateTriggerSo: Disparando SpawnRequestEvent com posição {origin}");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
        }

        public override void SetActive(bool active) => predicate?.SetActive(active);
        public override void Reset() => predicate?.Reset();
    }
}