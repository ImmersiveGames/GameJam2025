using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class PlanetSensor : MonoBehaviour
    {
        private const int MaxDetectionResults = 10;
        private const float DefaultRadius = 10f;
        private const float DefaultMinFrequency = 0.1f;
        private const float DefaultMaxFrequency = 0.5f;

        [SerializeField, Tooltip("Camada dos planetas a detectar")]
        protected LayerMask planetLayer;

        [SerializeField, Tooltip("Raio de detecção")]
        protected float radius = DefaultRadius;

        [SerializeField, Tooltip("Frequência mínima de verificação (segundos)")]
        protected float minDetectionFrequency = DefaultMinFrequency;

        [SerializeField, Tooltip("Frequência máxima de verificação (segundos)")]
        protected float maxDetectionFrequency = DefaultMaxFrequency;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        protected bool debugMode;

        protected Transform CachedTransform { get; private set; }
        protected float CurrentDetectionFrequency { get; set; }
        private float _detectionTimer;
        private readonly Collider[] _detectionResults = new Collider[MaxDetectionResults];

        public LayerMask PlanetLayer => planetLayer;

        protected virtual void Awake()
        {
            CachedTransform = transform;
            CurrentDetectionFrequency = maxDetectionFrequency;
        }

        protected virtual void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            _detectionTimer += Time.deltaTime;
            if (_detectionTimer < CurrentDetectionFrequency) return;

            ProcessPlanets(DetectPlanets());
            _detectionTimer = 0f;
        }

        private List<IPlanetInteractable> DetectPlanets()
        {
            var planets = new List<IPlanetInteractable>();
            int hitCount = Physics.OverlapSphereNonAlloc(CachedTransform.position, radius, _detectionResults, planetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var planet = GetPlanetsMasterInParent(_detectionResults[i]);
                if (planet is { IsActive: true })
                {
                    planets.Add(planet);
                }
            }

            return planets;
        }

        private IPlanetInteractable GetPlanetsMasterInParent(Component component)
        {
            if (!component) return null;

            // Tenta obter o componente no objeto atual
            if (component.TryGetComponent<IPlanetInteractable>(out var planet))
            {
                return planet;
            }

            // Se não encontrado, busca nos pais
            Transform current = component.transform.parent;
            while (current)
            {
                if (current.TryGetComponent(out planet))
                {
                    return planet;
                }
                current = current.parent;
            }

            return null;
        }

        protected abstract void ProcessPlanets(List<IPlanetInteractable> planets);

        protected virtual void AdjustDetectionFrequency(int planetCount)
        {
            CurrentDetectionFrequency = planetCount > 0
                ? Mathf.Lerp(maxDetectionFrequency, minDetectionFrequency, planetCount / 5f)
                : maxDetectionFrequency;
        }

        protected virtual void OnDrawGizmos()
        {
            if (!debugMode || !CachedTransform) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CachedTransform.position, radius);
        }
    }
}