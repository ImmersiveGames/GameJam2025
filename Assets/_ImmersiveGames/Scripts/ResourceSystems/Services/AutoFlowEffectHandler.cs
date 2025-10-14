using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    [DebugLevel(DebugLevel.Verbose)]
    public class AutoFlowEffectHandler : MonoBehaviour
    {
        [SerializeField] private string effectPoolName = "AutoFlowEffect";
        private EventBinding<AutoFlowEffectEvent> _effectBinding;

        private void Awake()
        {
            _effectBinding = new EventBinding<AutoFlowEffectEvent>(OnAutoFlowEffect);
            EventBus<AutoFlowEffectEvent>.Register(_effectBinding);
        }

        private void OnDestroy()
        {
            if (_effectBinding != null)
                EventBus<AutoFlowEffectEvent>.Unregister(_effectBinding);
        }

        private void OnAutoFlowEffect(AutoFlowEffectEvent evt)
        {
            var pool = PoolManager.Instance.GetPool(effectPoolName);
            if (pool == null)
            {
                DebugUtility.LogWarning<AutoFlowEffectHandler>($"Pool '{effectPoolName}' not found for AutoFlow effect.");
                return;
            }

            var effect = pool.GetObject(evt.Position, null, null, true);
            if (effect != null)
            {
                DebugUtility.LogVerbose<AutoFlowEffectHandler>($"Spawned effect for {evt.ActorId}.{evt.ResourceType} with delta {evt.Delta:F2}");
            }
        }
    }
}