using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    public class ResourceAutoFlowBridge : MonoBehaviour
    {
        [SerializeField] private bool startPaused = true;

        private ResourceAutoFlowService _autoFlow;
        private ResourceSystemService _resourceSystem;
        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>($"No IActor found on {name}. Disabling.");
                enabled = false;
                return;
            }
            string actorId = _actor.ActorId;
            if (!DependencyManager.Instance.TryGetForObject(actorId, out _resourceSystem))
            {
                var bridge = GetComponent<EntityResourceBridge>();
                if (bridge != null)
                    _resourceSystem = bridge.GetService();
            }

            if (_resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>($"ResourceAutoFlowBridge on {name} couldn't find ResourceSystemService. Disabling.");
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