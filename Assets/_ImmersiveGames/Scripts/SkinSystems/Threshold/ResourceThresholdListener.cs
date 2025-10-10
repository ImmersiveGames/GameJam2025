using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.SkinSystems.Threshold
{
    [System.Serializable]
    public class ThresholdConfig
    {
        [Header("Configuration")]
        public ResourceType resourceType = ResourceType.Health;
        [Range(0f, 1f)] public float threshold = 0.5f;
        public TriggerDirection direction = TriggerDirection.Descending;

        [Header("Actions")]
        public UnityEvent<float, float, bool> onThresholdCrossed;
    }

    public class ResourceThresholdListener : MonoBehaviour
    {
        private string _expectedActorId = ""; // Set no inspector para filtrar por ActorId

        [Header("Threshold Configurations")]
        [SerializeField] private ThresholdConfig[] thresholdConfigs;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private EventBinding<ResourceThresholdEvent> _thresholdBinding;

        #region Unity Lifecycle
        private void OnEnable()
        {
            _expectedActorId = GetComponentInParent<IActor>()?.ActorId ?? "";
            RegisterThresholdListener();
        }

        private void OnDisable()
        {
            UnregisterThresholdListener();
        }

        private void Start()
        {
            if (showDebugLogs) 
                Debug.Log($"[ResourceThresholdListener] Started listening with {thresholdConfigs.Length} configs on {gameObject.name}. Expected ActorId: {_expectedActorId}");

            for (int i = 0; i < thresholdConfigs.Length; i++)
            {
                var config = thresholdConfigs[i];
                int eventCount = config.onThresholdCrossed.GetPersistentEventCount();
                if (showDebugLogs)
                    Debug.Log($"[ResourceThresholdListener] Config {i} on {gameObject.name}: Type={config.resourceType}, Threshold={config.threshold}, Direction={config.direction}, Persistent Events={eventCount}");

                for (int j = 0; j < eventCount; j++)
                {
                    if (showDebugLogs)
                        Debug.Log($"[ResourceThresholdListener] Config {i} Event {j}: Target={config.onThresholdCrossed.GetPersistentTarget(j)?.GetType().Name}, Method={config.onThresholdCrossed.GetPersistentMethodName(j)}");
                }
            }
        }
        #endregion

        #region Event Handling
        private void RegisterThresholdListener()
        {
            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnResourceThreshold);
            EventBus<ResourceThresholdEvent>.Register(_thresholdBinding);
            if (showDebugLogs)
                Debug.Log($"[ResourceThresholdListener] Registered on EventBus on {gameObject.name}");
        }

        private void UnregisterThresholdListener()
        {
            EventBus<ResourceThresholdEvent>.Unregister(_thresholdBinding);
            if (showDebugLogs)
                Debug.Log($"[ResourceThresholdListener] Unregistered from EventBus on {gameObject.name}");
        }

        private void OnResourceThreshold(ResourceThresholdEvent evt)
        {
            if (!string.IsNullOrEmpty(_expectedActorId) && evt.ActorId != _expectedActorId)
            {
                if (showDebugLogs)
                    Debug.Log($"[ResourceThresholdListener] Event ignored on {gameObject.name}: ActorId {evt.ActorId} != expected {_expectedActorId}");
                return;
            }
            
            if (showDebugLogs) 
                Debug.Log($"[ResourceThresholdListener] Received event on {gameObject.name}: Actor={evt.ActorId}, Type={evt.ResourceType}, Threshold={evt.Threshold}, Percentage={evt.CurrentPercentage:P0}, Ascending={evt.IsAscending}");

            bool anyMatch = false;
            foreach (var config in thresholdConfigs)
            {
                if (showDebugLogs)
                    Debug.Log($"[ResourceThresholdListener] Checking config on {gameObject.name}: Type={config.resourceType}, Threshold={config.threshold}, Direction={config.direction}");

                if (evt.ResourceType == config.resourceType &&
                    Mathf.Approximately(evt.Threshold, config.threshold) &&
                    ShouldInvoke(config.direction, evt.IsAscending))
                {
                    anyMatch = true;
                    if (showDebugLogs)
                        Debug.Log($"[ResourceThresholdListener] Match found on {gameObject.name} - Invoking UnityEvent for threshold {evt.Threshold} (Persistent Events: {config.onThresholdCrossed.GetPersistentEventCount()})");
                    config.onThresholdCrossed?.Invoke(evt.Threshold, evt.CurrentPercentage, evt.IsAscending);
                }
                else if (showDebugLogs)
                {
                    Debug.Log($"[ResourceThresholdListener] No match on {gameObject.name} for this config (Threshold compare: evt={evt.Threshold}, config={config.threshold})");
                }
            }

            if (showDebugLogs && !anyMatch)
                Debug.Log($"[ResourceThresholdListener] No matching configs found on {gameObject.name} for event");
        }

        private bool ShouldInvoke(TriggerDirection dir, bool isAscending)
        {
            return (dir == TriggerDirection.Both) ||
                   (dir == TriggerDirection.Ascending && isAscending) ||
                   (dir == TriggerDirection.Descending && !isAscending);
        }
        #endregion
    }
}