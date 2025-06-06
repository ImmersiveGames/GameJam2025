using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class PlanetSensor : MonoBehaviour
    {
        [SerializeField, Tooltip("Camada dos planetas a detectar")]
        protected LayerMask planetLayer;

        [SerializeField, Tooltip("Raio de detecção")]
        protected float radius = 10f;

        [SerializeField, Tooltip("Frequência mínima de verificação (segundos)")]
        protected float minDetectionFrequency = 0.1f;

        [SerializeField, Tooltip("Frequência máxima de verificação (segundos)")]
        protected float maxDetectionFrequency = 0.5f;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        protected bool debugMode;

        protected Transform cachedTransform;
        protected float currentDetectionFrequency;
        
        private float _detectionTimer;
        private readonly Collider[] _detectionResults = new Collider[10];
        
        public LayerMask PlanetLayer => planetLayer;

        protected virtual void Awake()
        {
            cachedTransform = transform;
            currentDetectionFrequency = maxDetectionFrequency;
        }

        protected virtual void Update()
        {
            _detectionTimer += Time.deltaTime;
            if (!(_detectionTimer >= currentDetectionFrequency)) return;
            ProcessPlanets(DetectPlanets());
            _detectionTimer = 0f;
        }

        private List<PlanetsMaster> DetectPlanets()
        {
            var planets = new List<PlanetsMaster>();
            int hitCount = Physics.OverlapSphereNonAlloc(cachedTransform.position, radius, _detectionResults, planetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var planet = _detectionResults[i].GetComponentInParent<PlanetsMaster>();
                if (planet && planet.IsActive)
                {
                    planets.Add(planet);
                }
            }

            return planets;
        }

        protected abstract void ProcessPlanets(List<PlanetsMaster> planets);

        protected virtual void AdjustDetectionFrequency(int planetCount)
        {
            currentDetectionFrequency = planetCount > 0
                ? Mathf.Lerp(maxDetectionFrequency, minDetectionFrequency, planetCount / 5f)
                : maxDetectionFrequency;
        }

        protected virtual void OnDrawGizmos()
        {
            if (!debugMode || !cachedTransform) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cachedTransform.position, radius);
        }
    }
}