using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public class RingActivationSkin : SkinConfigurable
    {
        [Header("Ring Settings")]
        [SerializeField] private GameObject ringObject;
        [SerializeField] private float ringChance = 0.3f;
        [SerializeField] private bool applyOnSkinChange = true;
        [SerializeField] private bool enableRandomRotation = true;
        [SerializeField] private Vector2 rotationRange = new(0f, 360f);

        [Header("Initial State")]
        [SerializeField] private bool randomizeOnStart = true;
        [SerializeField] private bool randomizeOnSkinInstances = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private bool _hasRing;
        private float _currentRotation;
        private bool _isInitialized;

        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            
            if (randomizeOnStart)
            {
                RandomizeRing();
            }
            else
            {
                // Aplica o estado atual
                UpdateRingVisibility();
            }
            
            _isInitialized = true;
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Initialized - HasRing: {_hasRing}");
        }
        #endregion

        #region SkinConfigurable Implementation
        public override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (!applyOnSkinChange) return;
            
            RandomizeRing();
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Configured from skin: {_hasRing}, Rotation: {_currentRotation}°");
        }

        public override void ApplyDynamicModifications()
        {
            UpdateRingVisibility();
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Applied dynamic modifications");
        }

        protected override void OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType != targetModelType) return;
            
            if (randomizeOnSkinInstances)
            {
                RandomizeRing();
            }
            else
            {
                UpdateRingVisibility();
            }
        }
        #endregion

        #region Core Functionality
        private void UpdateRingVisibility()
        {
            if (ringObject != null)
            {
                bool wasActive = ringObject.activeSelf;
                ringObject.SetActive(_hasRing);
                
                if (_hasRing && enableRandomRotation)
                {
                    ApplyRingRotation();
                }
                
                if (showDebugLogs && wasActive != _hasRing)
                {
                    DebugUtility.LogVerbose<RingActivationSkin>($"Ring visibility changed: {wasActive} -> {_hasRing}");
                }
            }
            else if (_isInitialized && showDebugLogs)
            {
                DebugUtility.LogWarning<RingActivationSkin>("Ring object is not assigned!");
            }
        }

        private void ApplyRingRotation()
        {
            if (ringObject == null) return;

            var ringTransform = ringObject.transform;
            ringTransform.localRotation = Quaternion.Euler(0f, _currentRotation, 0f);
            
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Applied rotation: {_currentRotation}°");
        }
        #endregion

        #region Public API - Para Controle Externo
        /// <summary>
        /// Ativa ou desativa o anel
        /// </summary>
        public void SetRingVisible(bool visible)
        {
            _hasRing = visible;
            UpdateRingVisibility();
            
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Ring visibility set to: {visible}");
        }

        /// <summary>
        /// Gera uma nova configuração aleatória para o anel
        /// </summary>
        [ContextMenu("Randomize Ring")]
        public void RandomizeRing()
        {
            _hasRing = Random.value < ringChance;
            _currentRotation = enableRandomRotation ? Random.Range(rotationRange.x, rotationRange.y) : 0f;
            UpdateRingVisibility();
            
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Randomized - HasRing: {_hasRing}, Rotation: {_currentRotation}°");
        }

        /// <summary>
        /// Define uma rotação específica para o anel
        /// </summary>
        public void SetRingRotation(float rotation)
        {
            _currentRotation = rotation;
            if (_hasRing)
            {
                ApplyRingRotation();
            }
            
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Rotation set to: {rotation}°");
        }

        /// <summary>
        /// Define a chance do anel aparecer
        /// </summary>
        public void SetRingChance(float chance)
        {
            ringChance = Mathf.Clamp01(chance);
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Ring chance set to: {chance:P0}");
        }

        /// <summary>
        /// Alterna o estado do anel (liga/desliga)
        /// </summary>
        [ContextMenu("Toggle Ring")]
        public void ToggleRing()
        {
            _hasRing = !_hasRing;
            UpdateRingVisibility();
            
            if (showDebugLogs) DebugUtility.LogVerbose<RingActivationSkin>($"Ring toggled to: {_hasRing}");
        }

        /// <summary>
        /// Obtém o estado atual do anel
        /// </summary>
        public RingState GetRingState()
        {
            return new RingState
            {
                HasRing = _hasRing,
                Rotation = _currentRotation,
                IsVisible = ringObject != null && ringObject.activeSelf
            };
        }
        #endregion

        #region Editor Helpers
        #if UNITY_EDITOR
        [ContextMenu("Enable Ring")]
        private void EditorEnableRing()
        {
            SetRingVisible(true);
        }

        [ContextMenu("Disable Ring")]
        private void EditorDisableRing()
        {
            SetRingVisible(false);
        }

        [ContextMenu("Set Random Rotation")]
        private void EditorSetRandomRotation()
        {
            SetRingRotation(Random.Range(rotationRange.x, rotationRange.y));
        }
        #endif
        #endregion
    }

    [System.Serializable]
    public struct RingState
    {
        public bool HasRing;
        public float Rotation;
        public bool IsVisible;

        public override string ToString()
        {
            return $"Ring: {HasRing}, Rotation: {Rotation}°, Visible: {IsVisible}";
        }
    }
}