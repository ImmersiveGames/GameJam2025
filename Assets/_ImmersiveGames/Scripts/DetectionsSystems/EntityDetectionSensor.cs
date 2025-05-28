using System.Collections.Generic;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityDetectionSensor : MonoBehaviour
    {
        [SerializeField, Tooltip("Camada dos planetas a detectar")]
        private LayerMask planetLayer;

        [SerializeField, Tooltip("Raio de detecção externa (ativação de defesa)")]
        private float detectionRadius = 50f;

        [SerializeField, Tooltip("Raio de detecção interna (reconhecimento)")]
        private float recognitionRadius = 10f;

        [SerializeField, Tooltip("Ângulo máximo para reconhecimento (graus)")]
        private float recognitionAngle = 45f;

        [SerializeField, Tooltip("Frequência mínima de verificação (segundos)")]
        private float minDetectionFrequency = 0.1f;

        [SerializeField, Tooltip("Frequência máxima de verificação (segundos)")]
        private float maxDetectionFrequency = 0.5f;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        private bool debugMode;

        private IDetectable _detectableEntity;
        private Transform _cachedTransform;
        private float _detectionTimer;
        private float _currentDetectionFrequency;
        [SerializeField]private List<Planets> _detectedPlanets = new();
        [SerializeField]private List<Planets> _recognizedPlanets = new();
        private readonly Collider[] _detectionResults = new Collider[10]; // Ajuste o tamanho conforme necessário

        private void Awake()
        {
            _detectableEntity = GetComponent<IDetectable>();
            if (_detectableEntity == null)
            {
                DebugUtility.LogError<EntityDetectionSensor>("IDetectable não encontrado no GameObject.");
                enabled = false;
            }
            _cachedTransform = transform;
            _currentDetectionFrequency = maxDetectionFrequency;
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

        private void CheckDetection()
        {
            // Detectar planetas na área
            int hitCount = Physics.OverlapSphereNonAlloc(_cachedTransform.position, detectionRadius, _detectionResults, planetLayer);

            // Lista temporária para rastrear planetas atuais
            var currentPlanets = new List<Planets>();

            // Processar planetas detectados
            for (int i = 0; i < hitCount; i++)
            {
                var planet = _detectionResults[i].GetComponentInParent<Planets>();
                if (planet == null || !planet.IsActive) continue;

                currentPlanets.Add(planet);

                if (!_detectedPlanets.Contains(planet))
                {
                    // Novo planeta detectado
                    _detectedPlanets.Add(planet);
                    _detectableEntity.OnPlanetDetected(planet);
                    planet.GetComponent<IPlanetInteractable>()?.ActivateDefenses(_detectableEntity);
                    if (debugMode)
                    {
                        DebugUtility.LogVerbose<EntityDetectionSensor>($"Planeta detectado: {planet.name}", "green");
                    }
                }

                // Verificar reconhecimento
                CheckRecognition(planet);
            }

            // Verificar planetas que saíram da detecção
            for (int i = _detectedPlanets.Count - 1; i >= 0; i--)
            {
                var planet = _detectedPlanets[i];
                if (!currentPlanets.Contains(planet))
                {
                    _detectedPlanets.RemoveAt(i);
                    _recognizedPlanets.Remove(planet);
                    _detectableEntity.OnPlanetLost(planet);
                    if (debugMode)
                    {
                        DebugUtility.LogVerbose<EntityDetectionSensor>($"Planeta perdido: {planet.name}", "red");
                    }
                }
            }

            // Ajustar frequência de detecção
            _currentDetectionFrequency = _detectedPlanets.Count > 0
                ? Mathf.Lerp(maxDetectionFrequency, minDetectionFrequency, _detectedPlanets.Count / 5f)
                : maxDetectionFrequency;
        }

        private void CheckRecognition(Planets planet)
        {
            float distance = Vector3.Distance(
                new Vector3(_cachedTransform.position.x, 0f, _cachedTransform.position.z),
                new Vector3(planet.transform.position.x, 0f, planet.transform.position.z)
            );

            if (distance > recognitionRadius) return;

            // Verificar se está voltado para o planeta
            Vector3 directionToPlanet = (planet.transform.position - _cachedTransform.position).normalized;
            float angle = Vector3.Angle(_cachedTransform.forward, directionToPlanet);
            if (angle > recognitionAngle) return;

            if (!_recognizedPlanets.Contains(planet))
            {
                _recognizedPlanets.Add(planet);
                var planetInteractable = planet.GetComponent<IPlanetInteractable>();
                if (planetInteractable != null)
                {
                    _detectableEntity.OnRecognitionRangeEntered(planet, planetInteractable.GetResources());
                    planetInteractable.SendRecognitionData(_detectableEntity);
                    if (debugMode)
                    {
                        DebugUtility.LogVerbose<EntityDetectionSensor>($"Reconhecimento ativado para: {planet.name}", "blue");
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!debugMode || !_cachedTransform) return;

            // Desenhar raio de detecção
            Gizmos.color = _detectedPlanets.Count > 0 ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(_cachedTransform.position, detectionRadius);

            // Desenhar raio de reconhecimento
            Gizmos.color = _recognizedPlanets.Count > 0 ? Color.blue : Color.cyan;
            Gizmos.DrawWireSphere(_cachedTransform.position, recognitionRadius);

            // Desenhar linhas para planetas detectados
            foreach (var planet in _detectedPlanets)
            {
                Gizmos.color = _recognizedPlanets.Contains(planet) ? Color.blue : Color.red;
                Gizmos.DrawLine(_cachedTransform.position, planet.transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.transform.position + Vector3.up * 0.5f,
                    _recognizedPlanets.Contains(planet) ? $"Reconhecido: {planet.name}" : $"Detectado: {planet.name}");
#endif
            }
        }
    }
}