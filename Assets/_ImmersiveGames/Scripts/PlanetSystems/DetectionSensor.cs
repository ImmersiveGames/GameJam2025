using System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class DetectionSensor : MonoBehaviour
    {
        [SerializeField, Tooltip("Camada dos alvos a detectar")]
        private LayerMask targetLayer;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        private bool debugMode;

        [SerializeField, Tooltip("Frequência mínima de verificação (segundos)")]
        private float minDetectionFrequency = 0.1f;

        [SerializeField, Tooltip("Frequência máxima de verificação (segundos)")]
        private float maxDetectionFrequency = 0.5f;

        public event Action<PlayerInput> OnTargetEnterDetection;
        public event Action<PlayerInput> OnTargetExitDetection;

        private float _detectionRadius;
        private PlayerInput _detectedTarget;
        private bool _isInDetectionRange;
        private Transform _cachedTransform;
        private float _detectionTimer;
        private float _currentDetectionFrequency;
        private readonly Collider[] _detectionResults = new Collider[1];
        
        private Planets _planet;
        private void Awake()
        {
            _planet = GetComponent<Planets>();
        }
        private void OnEnable()
        {
            if (!_planet) return;
            _planet.EventPlanetCreated += HandlePlanetCreated;
            _planet.EventPlanetDestroyed += HandlePlanetDestroyed;
        }

        private void OnDisable()
        {
            if (!_planet) return;
            _planet.EventPlanetCreated -= HandlePlanetCreated;
            _planet.EventPlanetDestroyed -= HandlePlanetDestroyed;
        }

        private void Update()
        {
            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= _currentDetectionFrequency)
            {
                CheckDetection();
                _detectionTimer = 0f;
            }
        }

        private void HandlePlanetCreated(int id, PlanetData data, PlanetResourcesSo resourcesSo)
        {
            _cachedTransform = transform;
            _detectionRadius = data.detectionRadius;
            _currentDetectionFrequency = minDetectionFrequency;
        }

        private void HandlePlanetDestroyed(int id)
        {
            enabled = false;
            HandleNoTargetDetected();
            if (debugMode)
            {
                DebugUtility.LogVerbose<DetectionSensor>($"Planeta destruído, sensor desativado em {gameObject.name}.", "red");
            }
        }
        

        private void CheckDetection()
        {
            if (!_planet.IsActive) return;
            int hitCount = Physics.OverlapSphereNonAlloc(_cachedTransform.position, _detectionRadius, _detectionResults, targetLayer);

            if (hitCount == 0)
            {
                HandleNoTargetDetected();
                return;
            }

            var detectedCollider = _detectionResults[0];
            if (!detectedCollider || !HandleTargetDetection(detectedCollider))
            {
                HandleNoTargetDetected();
            }
        }

        private bool HandleTargetDetection(Collider detectedCollider)
        {
            var target = detectedCollider.GetComponentInParent<PlayerInput>();
            if (!target) return false;

            float distance = Vector3.Distance(
                new Vector3(_cachedTransform.position.x, 0f, _cachedTransform.position.z),
                new Vector3(detectedCollider.transform.position.x, 0f, detectedCollider.transform.position.z)
            );

            if (!(distance <= _detectionRadius)) return false;

            // Ajustar frequência de detecção com base na distância
            _currentDetectionFrequency = Mathf.Lerp(maxDetectionFrequency, minDetectionFrequency, distance / _detectionRadius);

            if (_isInDetectionRange && _detectedTarget == target) return true;
            _isInDetectionRange = true;
            _detectedTarget = target;
            OnTargetEnterDetection?.Invoke(target);
            if (debugMode)
            {
                DebugUtility.LogVerbose<DetectionSensor>($"Entrou no raio de detecção: {target.name} (Distância: {distance:F2})", "green");
            }
            return true;
        }

        private void HandleNoTargetDetected()
        {
            // Resetar para frequência máxima quando não há alvo
            _currentDetectionFrequency = maxDetectionFrequency;

            if (!_isInDetectionRange) return;
            _isInDetectionRange = false;
            if (!_detectedTarget) return;
            OnTargetExitDetection?.Invoke(_detectedTarget);
            if (debugMode)
            {
                DebugUtility.LogVerbose<DetectionSensor>($"Saiu do raio de detecção: {_detectedTarget.name}", "red");
            }
            _detectedTarget = null;
        }

        private void OnDrawGizmos()
        {
            if (!debugMode || !_cachedTransform) return;

            Gizmos.color = _isInDetectionRange ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(_cachedTransform.position, _detectionRadius);

            if (!_isInDetectionRange || !_detectedTarget) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_cachedTransform.position, _detectedTarget.transform.position);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(_cachedTransform.position + Vector3.up * 0.5f, $"Detectado: {_detectedTarget.name}");
#endif
        }
    }
}