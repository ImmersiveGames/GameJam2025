using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    [DefaultExecutionOrder(-2)]
    public class ResourceAutoFlowBridge : MonoBehaviour
    {
        [SerializeField] private bool startPaused = true;

        private ResourceAutoFlowService _autoFlow;
        private ResourceSystemService _resourceSystem;

        private void Awake()
        {
            var actorId = gameObject.name;
            if (!DependencyManager.Instance.TryGetForObject(actorId, out _resourceSystem))
            {
                var bridge = GetComponent<EntityResourceBridge>();
                if (bridge != null)
                    _resourceSystem = bridge.GetService();
            }

            if (_resourceSystem == null)
            {
                Debug.LogWarning($"ResourceAutoFlowBridge on {name} couldn't find ResourceSystemService. Disabling.");
                enabled = false;
                return;
            }

            _autoFlow = new ResourceAutoFlowService(_resourceSystem, startPaused);
        }

        private void Update()
        {
            _autoFlow?.Process(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _autoFlow?.Dispose();
            _autoFlow = null;
        }

        [ContextMenu("AutoFlow: Pause")]
        private void ContextPause() => _autoFlow?.Pause();

        [ContextMenu("AutoFlow: Resume")]
        private void ContextResume() => _autoFlow?.Resume();

        [ContextMenu("AutoFlow: Toggle")]
        private void ContextToggle() => _autoFlow?.Toggle();

        [ContextMenu("AutoFlow: Reset Timers")]
        private void ContextReset() => _autoFlow?.ResetTimers();
    }
}