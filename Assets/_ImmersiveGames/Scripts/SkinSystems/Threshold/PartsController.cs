using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Threshold
{
    public class PartsController : MonoBehaviour
    {
        [Header("Parts Configuration")]
        [SerializeField] private GameObject[] parts;

        [Header("References")]
        [SerializeField] private ResourceThresholdListener thresholdListener;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private bool _isInitialized;

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (_isInitialized) return;

            SetAllParts(true);
            if (showDebugLogs)
                Debug.Log($"[PartsController] Initial state set to all active on {gameObject.name} for 100% health");

            _isInitialized = true;
            if (showDebugLogs)
                Debug.Log($"[PartsController] Initialized with {parts.Length} parts on {gameObject.name}");
        }
        #endregion

        #region Public Methods for UnityEvents
        // Método unificado para registro em UnityEvents (suporta Both)
        public void HandleThresholdCrossed(float threshold, float percentage, bool ascending)
        {
            if (showDebugLogs)
                Debug.Log($"[PartsController] Entering HandleThresholdCrossed on {gameObject.name}: Threshold={threshold}, Percentage={percentage:P0}, Ascending={ascending}");
            
            UpdatePartsState(percentage, ascending); // Roteia baseado em direção

            if (showDebugLogs)
                Debug.Log($"[PartsController] Handled threshold at {threshold} (Ascending={ascending}) on {gameObject.name}");
        }
        #endregion

        #region Parts Management
        private void UpdatePartsState(float healthPercentage, bool isAscending)
        {
            if (showDebugLogs)
                Debug.Log($"[PartsController] Updating parts state on {gameObject.name}: Percentage={healthPercentage:P0}, IsAscending={isAscending}");

            if (!isAscending)
            {
                UpdatePartsDeactivation(healthPercentage);
            }
            else
            {
                UpdatePartsActivation(healthPercentage);
            }

            if (showDebugLogs)
                Debug.Log($"[PartsController] Parts state updated on {gameObject.name}");
        }

        private void UpdatePartsDeactivation(float healthPercentage)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null)
                {
                    float partThreshold = 1f - ((i + 1f) / parts.Length);
                    parts[i].SetActive(healthPercentage > partThreshold);
                    if (showDebugLogs)
                        Debug.Log($"[PartsController] Deactivation: Part {i} ({parts[i].name}) set to {parts[i].activeSelf} (threshold={partThreshold}) on {gameObject.name}");
                }
            }
        }

        private void UpdatePartsActivation(float healthPercentage)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null)
                {
                    float partThreshold = 1f - ((i + 1f) / parts.Length);
                    parts[i].SetActive(healthPercentage > partThreshold); // Mantido para consistência em reativação quando high
                    if (showDebugLogs)
                        Debug.Log($"[PartsController] Activation: Part {i} ({parts[i].name}) set to {parts[i].activeSelf} (threshold={partThreshold}) on {gameObject.name}");
                }
            }
        }

        private void SetPartActive(int partIndex, bool active)
        {
            if (IsValidPartIndex(partIndex) && parts[partIndex] != null)
            {
                parts[partIndex].SetActive(active);
                if (showDebugLogs)
                    Debug.Log($"[PartsController] Set part {partIndex} to {active} on {gameObject.name}");
            }
        }

        private void SetAllParts(bool active)
        {
            foreach (var part in parts)
            {
                if (part != null)
                {
                    part.SetActive(active);
                    if (showDebugLogs)
                        Debug.Log($"[PartsController] Set part {part.name} to {active} on {gameObject.name}");
                }
            }
        }

        private bool IsValidPartIndex(int index)
        {
            return index >= 0 && index < parts.Length;
        }
        #endregion

        #region Public API
        public void Reset()
        {
            SetAllParts(true);
            if (showDebugLogs)
                Debug.Log($"[PartsController] Reset to initial state (all active, health=100%) on {gameObject.name}");
        }
        #endregion
    }

    public enum TriggerDirection
    {
        Ascending,
        Descending,
        Both
    }
}