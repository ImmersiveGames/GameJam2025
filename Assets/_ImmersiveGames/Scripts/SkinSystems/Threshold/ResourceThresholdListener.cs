using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.SkinSystems.Threshold
{
    [System.Serializable]
    public class ThresholdConfig
    {
        [Header("Configuration")]
        public RuntimeAttributeType runtimeAttributeType = RuntimeAttributeType.Health;
        [Range(0f, 1f)] public float threshold = 0.5f;
        public TriggerDirection direction = TriggerDirection.Descending;

        [Header("Actions")]
        public UnityEvent<float, float, bool> onThresholdCrossed;
    }

    public class ResourceThresholdListener : MonoBehaviour
    {
        private string _expectedActorId = ""; // Set no inspector para filtrar por ActorId
        private object _registrationScope;

        [Header("Threshold Configurations")]
        [SerializeField] private ThresholdConfig[] thresholdConfigs;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private EventBinding<RuntimeAttributeThresholdEvent> _thresholdBinding;

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
                DebugUtility.LogVerbose<ResourceThresholdListener>($"Started listening with {thresholdConfigs.Length} configs on {gameObject.name}. Expected ActorId: {_expectedActorId}");

            for (int i = 0; i < thresholdConfigs.Length; i++)
            {
                var config = thresholdConfigs[i];
                int eventCount = config.onThresholdCrossed.GetPersistentEventCount();
                if (showDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"Config {i} on {gameObject.name}: Type={config.runtimeAttributeType}, Threshold={config.threshold}, Direction={config.direction}, Persistent Events={eventCount}");

                for (int j = 0; j < eventCount; j++)
                {
                    if (showDebugLogs)
                        DebugUtility.LogVerbose<ResourceThresholdListener>($"Config {i} Event {j}: Target={config.onThresholdCrossed.GetPersistentTarget(j)?.GetType().Name}, Method={config.onThresholdCrossed.GetPersistentMethodName(j)}");
                }
            }
        }
        #endregion

        #region Event Handling
        private void RegisterThresholdListener()
        {
            _thresholdBinding = new EventBinding<RuntimeAttributeThresholdEvent>(OnResourceThreshold);

            if (string.IsNullOrEmpty(_expectedActorId))
            {
                EventBus<RuntimeAttributeThresholdEvent>.Register(_thresholdBinding);
                if (showDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"Registered globally on EventBus (sem ActorId) em {gameObject.name}");
                return;
            }

            _registrationScope = _expectedActorId;
            FilteredEventBus<RuntimeAttributeThresholdEvent>.Register(_thresholdBinding, _registrationScope);
            if (showDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdListener>($"Registered on FilteredEventBus para ActorId {_expectedActorId} em {gameObject.name}");
        }

        private void UnregisterThresholdListener()
        {
            if (_thresholdBinding == null)
                return;

            if (_registrationScope != null)
            {
                FilteredEventBus<RuntimeAttributeThresholdEvent>.Unregister(_thresholdBinding, _registrationScope);
                _registrationScope = null;
                if (showDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"Unregistered from FilteredEventBus em {gameObject.name}");
                _thresholdBinding = null;
                return;
            }

            EventBus<RuntimeAttributeThresholdEvent>.Unregister(_thresholdBinding);
            if (showDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdListener>($"Unregistered from EventBus on {gameObject.name}");
            _thresholdBinding = null;
        }

        private void OnResourceThreshold(RuntimeAttributeThresholdEvent evt)
        {
            if (!string.IsNullOrEmpty(_expectedActorId) && evt.ActorId != _expectedActorId)
            {
                if (showDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"Event ignored on {gameObject.name}: ActorId {evt.ActorId} != expected {_expectedActorId}");
                return;
            }
            
            if (showDebugLogs) 
                DebugUtility.LogVerbose<ResourceThresholdListener>($"Received event on {gameObject.name}: Actor={evt.ActorId}, Type={evt.RuntimeAttributeType}, Threshold={evt.Threshold}, Percentage={evt.CurrentPercentage:P0}, Ascending={evt.IsAscending}");

            bool anyMatch = false;
            foreach (var config in thresholdConfigs)
            {
                if (showDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"Checking config on {gameObject.name}: Type={config.runtimeAttributeType}, Threshold={config.threshold}, Direction={config.direction}");

                if (evt.RuntimeAttributeType == config.runtimeAttributeType &&
                    Mathf.Approximately(evt.Threshold, config.threshold) &&
                    ShouldInvoke(config.direction, evt.IsAscending))
                {
                    anyMatch = true;
                    if (showDebugLogs)
                        DebugUtility.LogVerbose<ResourceThresholdListener>($"Match found on {gameObject.name} - Invoking UnityEvent for threshold {evt.Threshold} (Persistent Events: {config.onThresholdCrossed.GetPersistentEventCount()})");
                    config.onThresholdCrossed?.Invoke(evt.Threshold, evt.CurrentPercentage, evt.IsAscending);
                }
                else if (showDebugLogs)
                {
                    DebugUtility.LogVerbose<ResourceThresholdListener>($"No match on {gameObject.name} for this config (Threshold compare: evt={evt.Threshold}, config={config.threshold})");
                }
            }

            if (showDebugLogs && !anyMatch)
                DebugUtility.LogVerbose<ResourceThresholdListener>($"No matching configs found on {gameObject.name} for event");
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