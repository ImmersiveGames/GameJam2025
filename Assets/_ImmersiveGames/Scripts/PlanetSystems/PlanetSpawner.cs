using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab do planeta a ser instanciado")]
        private GameObject planetPrefab;

        [SerializeField, Tooltip("Configuração geral do jogo")]
        private GameConfig gameConfig;

        [SerializeField, Tooltip("Centro do universo em torno do qual os planetas orbitam")]
        private Transform universeCenter;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        private bool debugMode;

        private PlanetResourceManager _resourceManager;
        private readonly List<Planets> _activePlanets = new List<Planets>();

        private void Awake()
        {
            _resourceManager = GetComponent<PlanetResourceManager>();
            if (_resourceManager == null)
            {
                DebugUtility.LogError<PlanetSpawner>("PlanetResourceManager não encontrado.", this);
                enabled = false;
            }

            if (universeCenter == null)
            {
                DebugUtility.LogError<PlanetSpawner>("UniverseCenter não atribuído.", this);
            }

            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetSpawner>("PlanetPrefab não atribuído.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GameManager.Instance.EventStartGame += SpawnPlanets;
        }

        private void OnDisable()
        {
            GameManager.Instance.EventStartGame -= SpawnPlanets;
        }

        private void SpawnPlanets()
        {
            if (gameConfig == null)
            {
                DebugUtility.LogError<PlanetSpawner>("GameConfig não está definido.", this);
                return;
            }

            int minPlanets = 2 * System.Enum.GetValues(typeof(PlanetResources)).Length;
            if (gameConfig.numPlanets < minPlanets)
            {
                DebugUtility.LogWarning<PlanetSpawner>($"Número de planetas ({gameConfig.numPlanets}) menor que o mínimo ({minPlanets}). Ajustando.", this);
                gameConfig.numPlanets = minPlanets;
            }

            // Destruir planetas ativos existentes
            foreach (var planet in _activePlanets)
            {
                planet.OnDeath -= OnPlanetDestroyed;
                if (planet != null)
                {
                    Destroy(planet.gameObject);
                }
            }
            _activePlanets.Clear();

            var resources = _resourceManager.GenerateResourceList(gameConfig.numPlanets);
            float currentRadius = gameConfig.minOrbitRadius;
            List<(Vector3 position, PlanetData data)> planetPositions = new List<(Vector3, PlanetData)>();

            for (int i = 0; i < gameConfig.numPlanets; i++)
            {
                PlanetData planetData = _resourceManager.GetRandomPlanetData();
                if (planetData == null)
                {
                    DebugUtility.LogError<PlanetSpawner>($"Nenhum PlanetData válido para o planeta {i + 1}.", this);
                    continue;
                }

                float angle = Random.Range(0f, 360f);
                Vector3 position = new Vector3(
                    universeCenter.position.x + Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius,
                    universeCenter.position.y,
                    universeCenter.position.z + Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius
                );

                planetPositions.Add((position, planetData));
                float spacing = planetData.size + gameConfig.orbitMargin;
                currentRadius += spacing;

                if (debugMode)
                {
                    DebugUtility.LogVerbose<PlanetSpawner>($"Planeta {i + 1}: Raio={currentRadius:F2}, Tamanho={planetData.size:F2}, Margem={gameConfig.orbitMargin:F2}, Posição={position}", "cyan");
                }
            }

            for (int i = 0; i < planetPositions.Count; i++)
            {
                var (position, planetData) = planetPositions[i];
                // Instanciar planeta usando o planetPrefab
                GameObject planetObj = Instantiate(planetPrefab, position, Quaternion.identity);
                if (planetObj == null)
                {
                    DebugUtility.LogWarning<PlanetSpawner>($"Falha ao instanciar planeta {i + 1}.", this);
                    continue;
                }

                planetObj.name = $"Planeta_{i + 1}";
                Planets planet = planetObj.GetComponent<Planets>();
                if (planet == null)
                {
                    DebugUtility.LogWarning<PlanetSpawner>($"Planeta {i + 1} não tem componente Planets.", this);
                    Destroy(planetObj);
                    continue;
                }

                // Configurar o planeta
                planet.SetPlanetData(planetData);
                planet.SetResource(resources[i]);
                planet.StartOrbit(universeCenter);
                planet.Initialize();
                planet.OnDeath += OnPlanetDestroyed;
                _activePlanets.Add(planet);

                if (debugMode)
                {
                    DebugUtility.LogVerbose<PlanetSpawner>($"Planeta {i + 1} spawnado: Posição={position}, Recurso={resources[i]}", "green");
                }
            }
        }

        private void OnPlanetDestroyed(DestructibleObject planet)
        {
            _activePlanets.Remove(planet as Planets);
            if (planet != null)
            {
                planet.gameObject.SetActive(false);
            }
        }

        public List<Planets> GetActivePlanets() => _activePlanets;

        private void OnDrawGizmos()
        {
            if (!debugMode || _activePlanets.Count == 0) return;

            foreach (var planet in _activePlanets)
            {
                if (planet != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(planet.transform.position, 0.5f);
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(planet.transform.position + Vector3.up * 0.5f,
                        $"Planeta: {planet.gameObject.name}\nRecurso: {planet.Resource}");
#endif
                }
            }
        }
    }
}