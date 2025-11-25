using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public class RandomTransformSkin : SkinConfigurable
    {
        [Header("Scale Settings")]
        [SerializeField] private Vector3 minScale = new(0.8f, 0.8f, 0.8f);
        [SerializeField] private Vector3 maxScale = new(1.2f, 1.2f, 1.2f);
        [SerializeField] private bool uniformScaling = true;
        [SerializeField] private bool applyScaleOnStart = true;
        [SerializeField] private bool reapplyScaleOnNewSkin = true;
        [SerializeField] private bool preserveOriginalScale = true;

        [Header("Rotation Settings")]
        [SerializeField] private bool applyRandomRotation = true;
        [SerializeField] private Vector3 minRotation = Vector3.zero;
        [SerializeField] private Vector3 maxRotation = new(360f, 360f, 360f);
        [SerializeField] private bool uniformRotation;
        [SerializeField] private bool applyRotationOnStart = true;
        [SerializeField] private bool reapplyRotationOnNewSkin = true;

        [Header("Initial State")]
        [SerializeField] private bool randomizeOnSkinInstances = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private readonly Dictionary<GameObject, Vector3> _originalScales = new();
        private readonly Dictionary<GameObject, Quaternion> _originalRotations = new();
        
        private Vector3 _currentRandomScale;
        private Vector3 _currentRandomRotation;


        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            
            if (applyScaleOnStart || applyRotationOnStart)
            {
                ApplyRandomTransform();
            }
            if (showDebugLogs) DebugUtility.LogVerbose<RandomTransformSkin>($"Initialized");
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
            foreach (var instance in instances.Where(instance => instance != null))
            {
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
            
            if (showDebugLogs) DebugUtility.LogVerbose<RandomTransformSkin>($"Applied transform - Scale: {_currentRandomScale}, Rotation: {_currentRandomRotation}");
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
                float rotation = Random.Range(minRotation.x, maxRotation.x);
                _currentRandomRotation = new Vector3(rotation, rotation, rotation);
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
            List<GameObject> instances = GetSkinInstances();
            if (instances == null) 
            {
                if (showDebugLogs) DebugUtility.LogWarning<RandomTransformSkin>("[RandomTransformSkin] No instances found");
                return;
            }

            CacheOriginalTransforms(instances);
            ApplyCurrentTransformToInstances();
        }

        private void ApplyCurrentTransformToInstances()
        {
            List<GameObject> instances = GetSkinInstances();
            if (instances == null) return;

            foreach (var instance in instances.Where(instance => instance != null))
            {
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
            foreach (KeyValuePair<GameObject, Vector3> kvp in _originalScales)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localScale = kvp.Value;
                }
            }

            foreach (KeyValuePair<GameObject, Quaternion> kvp in _originalRotations)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localRotation = kvp.Value;
                }
            }
            
            if (showDebugLogs) DebugUtility.LogVerbose<RandomTransformSkin>("[Reset to original transform");
        }

        /// <summary>
        /// Obtém a transformação aleatória atual
        /// </summary>
        public TransformState GetCurrentTransformState()
        {
            return new TransformState
            {
                scale = _currentRandomScale,
                rotation = _currentRandomRotation
            };
        }
        #endregion

        #region Utility Methods
        private void ApplyScaleToInstances()
        {
            List<GameObject> instances = GetSkinInstances();
            if (instances == null) return;

            foreach (var instance in instances)
            {
                ApplyScaleToInstance(instance);
            }
        }

        private void ApplyRotationToInstances()
        {
            List<GameObject> instances = GetSkinInstances();
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
            DebugUtility.LogVerbose<RandomTransformSkin>($"Scale: {state.scale}, Rotation: {state.rotation}");
        }
        #endif
        #endregion
    }

    [System.Serializable]
    public struct TransformState
    {
        public Vector3 scale;
        public Vector3 rotation;

        public override string ToString()
        {
            return $"Scale: {scale}, Rotation: {rotation}";
        }
    }
}