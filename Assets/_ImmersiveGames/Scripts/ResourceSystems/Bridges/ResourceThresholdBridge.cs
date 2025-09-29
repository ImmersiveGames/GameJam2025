using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    public class ResourceThresholdBridge : MonoBehaviour
    {
        private ResourceThresholdService _thresholdService;
        private ResourceSystemService _resourceSystem;
        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                Debug.LogWarning($"[ResourceThresholdBridge] No IActor found on {name}. Disabling.");
                enabled = false;
                return;
            }

            string actorId = _actor.ActorId;
            if (!DependencyManager.Instance.TryGetForObject(actorId, out _resourceSystem))
            {
                var bridge = GetComponent<EntityResourceBridge>();
                if (bridge != null) _resourceSystem = bridge.GetService();
            }

            if (_resourceSystem == null)
            {
                Debug.LogWarning($"ResourceThresholdBridge on {name} couldn't find ResourceSystemService. Disabling.");
                enabled = false;
                return;
            }

            _thresholdService = new ResourceThresholdService(_resourceSystem);
        }

        private void OnDestroy()
        {
            _thresholdService?.Dispose();
            _thresholdService = null;
        }

        [ContextMenu("Force Threshold Check")]
        private void ContextForce() => _thresholdService?.ForceCheck();
    }
}