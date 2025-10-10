using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public class RandomTransformSkin : SkinConfigurable
    {
        [Header("Scale Settings")]
        [SerializeField] private Vector3 minScale = new Vector3(0.8f, 0.8f, 0.8f);
        [SerializeField] private Vector3 maxScale = new Vector3(1.2f, 1.2f, 1.2f);
        [SerializeField] private bool uniformScaling = true;
        [SerializeField] private bool applyScaleOnStart = true;
        [SerializeField] private bool reapplyScaleOnNewSkin = true;
        [SerializeField] private bool preserveOriginalScale = true;

        [Header("Rotation Settings")]
        [SerializeField] private bool applyRandomRotation = true;
        [SerializeField] private Vector3 minRotation = Vector3.zero;
        [SerializeField] private Vector3 maxRotation = new Vector3(360f, 360f, 360f);
        [SerializeField] private bool uniformRotation = false;
        [SerializeField] private bool applyRotationOnStart = true;
        [SerializeField] private bool reapplyRotationOnNewSkin = true;

        [Header("Initial State")]
        [SerializeField] private bool randomizeOnSkinInstances = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
        private Dictionary<GameObject, Quaternion> _originalRotations = new Dictionary<GameObject, Quaternion>();
        
        private Vector3 _currentRandomScale;
        private Vector3 _currentRandomRotation;
        private bool _isInitialized = false;

        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            
            if (applyScaleOnStart || applyRotationOnStart)
            {
                ApplyRandomTransform();
            }
            
            _isInitialized = true;
            if (showDebugLogs) Debug.Log($"[RandomTransformSkin] Initialized");
        }
        #endregion

        #region SkinConfigurable Implementation
        public override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (reapplyScaleOnNewSkin || reapplyRotationOnNewSkin)
            {
                ApplyRandomTransform();
            }
        }

        public override void ApplyDynamicModifications()
        {
            ApplyRandomTransform();
        }

        protected override void OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType != targetModelType) return;
            
            CacheOriginalTransforms(instances);
            
            if (randomizeOnSkinInstances)
            {
                ApplyRandomTransform();
            }
            else
            {
                ApplyCurrentTransformToInstances();
            }
        }
        #endregion

        #region Core Functionality
        private void CacheOriginalTransforms(List<GameObject> instances)
        {
            foreach (var instance in instances)
            {
                if (instance == null) continue;

                if (preserveOriginalScale && !_originalScales.ContainsKey(instance))
                {
                    _originalScales[instance] = instance.transform.localScale;
                }

                if (!_originalRotations.ContainsKey(instance))
                {
                    _originalRotations[instance] = instance.transform.localRotation;
                }
            }
        }

        [ContextMenu("Randomize Transform")]
        public void ApplyRandomTransform()
        {
            GenerateRandomScale();
            GenerateRandomRotation();
            ApplyTransformToInstances();
            
            if (showDebugLogs) Debug.Log($"[RandomTransformSkin] Applied transform - Scale: {_currentRandomScale}, Rotation: {_currentRandomRotation}");
        }

        private void GenerateRandomScale()
        {
            if (uniformScaling)
            {
                float uniformScale = Random.Range(minScale.x, maxScale.x);
                _currentRandomScale = new Vector3(uniformScale, uniformScale, uniformScale);
            }
            else
            {
                _currentRandomScale = new Vector3(
                    Random.Range(minScale.x, maxScale.x),
                    Random.Range(minScale.y, maxScale.y),
                    Random.Range(minScale.z, maxScale.z)
                );
            }
        }

        private void GenerateRandomRotation()
        {
            if (!applyRandomRotation)
            {
                _currentRandomRotation = Vector3.zero;
                return;
            }

            if (uniformRotation)
            {
                float uniformRotation = Random.Range(minRotation.x, maxRotation.x);
                _currentRandomRotation = new Vector3(uniformRotation, uniformRotation, uniformRotation);
            }
            else
            {
                _currentRandomRotation = new Vector3(
                    Random.Range(minRotation.x, maxRotation.x),
                    Random.Range(minRotation.y, maxRotation.y),
                    Random.Range(minRotation.z, maxRotation.z)
                );
            }
        }

        private void ApplyTransformToInstances()
        {
            var instances = GetSkinInstances();
            if (instances == null) 
            {
                if (showDebugLogs) Debug.LogWarning("[RandomTransformSkin] No instances found");
                return;
            }

            CacheOriginalTransforms(instances);
            ApplyCurrentTransformToInstances();
        }

        private void ApplyCurrentTransformToInstances()
        {
            var instances = GetSkinInstances();
            if (instances == null) return;

            foreach (var instance in instances)
            {
                if (instance == null) continue;

                ApplyScaleToInstance(instance);
                ApplyRotationToInstance(instance);
            }
        }

        private void ApplyScaleToInstance(GameObject instance)
        {
            if (preserveOriginalScale)
            {
                if (!_originalScales.ContainsKey(instance))
                {
                    _originalScales[instance] = instance.transform.localScale;
                }

                Vector3 originalScale = _originalScales[instance];
                instance.transform.localScale = new Vector3(
                    originalScale.x * _currentRandomScale.x,
                    originalScale.y * _currentRandomScale.y,
                    originalScale.z * _currentRandomScale.z
                );
            }
            else
            {
                instance.transform.localScale = _currentRandomScale;
            }
        }

        private void ApplyRotationToInstance(GameObject instance)
        {
            if (!applyRandomRotation) return;

            if (!_originalRotations.ContainsKey(instance))
            {
                _originalRotations[instance] = instance.transform.localRotation;
            }

            Quaternion originalRotation = _originalRotations[instance];
            Quaternion randomRotation = Quaternion.Euler(_currentRandomRotation);
            instance.transform.localRotation = originalRotation * randomRotation;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Gera e aplica uma nova transformação aleatória
        /// </summary>
        public void RandomizeTransform()
        {
            ApplyRandomTransform();
        }

        /// <summary>
        /// Define uma escala específica
        /// </summary>
        public void SetSpecificScale(Vector3 scale)
        {
            _currentRandomScale = scale;
            ApplyScaleToInstances();
        }

        /// <summary>
        /// Define uma rotação específica
        /// </summary>
        public void SetSpecificRotation(Vector3 rotation)
        {
            _currentRandomRotation = rotation;
            ApplyRotationToInstances();
        }

        /// <summary>
        /// Define os limites de escala
        /// </summary>
        public void SetScaleRange(Vector3 newMinScale, Vector3 newMaxScale)
        {
            minScale = newMinScale;
            maxScale = newMaxScale;
        }

        /// <summary>
        /// Define os limites de rotação
        /// </summary>
        public void SetRotationRange(Vector3 newMinRotation, Vector3 newMaxRotation)
        {
            minRotation = newMinRotation;
            maxRotation = newMaxRotation;
        }

        /// <summary>
        /// Reseta todas as instâncias para suas transformações originais
        /// </summary>
        [ContextMenu("Reset Transform")]
        public void ResetToOriginalTransform()
        {
            foreach (var kvp in _originalScales)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localScale = kvp.Value;
                }
            }

            foreach (var kvp in _originalRotations)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localRotation = kvp.Value;
                }
            }
            
            if (showDebugLogs) Debug.Log("[RandomTransformSkin] Reset to original transform");
        }

        /// <summary>
        /// Obtém a transformação aleatória atual
        /// </summary>
        public TransformState GetCurrentTransformState()
        {
            return new TransformState
            {
                Scale = _currentRandomScale,
                Rotation = _currentRandomRotation
            };
        }
        #endregion

        #region Utility Methods
        private void ApplyScaleToInstances()
        {
            var instances = GetSkinInstances();
            if (instances == null) return;

            foreach (var instance in instances)
            {
                ApplyScaleToInstance(instance);
            }
        }

        private void ApplyRotationToInstances()
        {
            var instances = GetSkinInstances();
            if (instances == null) return;

            foreach (var instance in instances)
            {
                ApplyRotationToInstance(instance);
            }
        }
        #endregion

        #region Editor Helpers
        #if UNITY_EDITOR
        [ContextMenu("Log Current State")]
        private void EditorLogState()
        {
            var state = GetCurrentTransformState();
            Debug.Log($"Scale: {state.Scale}, Rotation: {state.Rotation}");
        }
        #endif
        #endregion
    }

    [System.Serializable]
    public struct TransformState
    {
        public Vector3 Scale;
        public Vector3 Rotation;

        public override string ToString()
        {
            return $"Scale: {Scale}, Rotation: {Rotation}";
        }
    }
}