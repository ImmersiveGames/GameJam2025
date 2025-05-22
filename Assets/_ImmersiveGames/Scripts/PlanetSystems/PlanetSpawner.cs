using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ScriptableObjects;
using ImmersiveGames.EnemySystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab do planeta com lógica padrão")]
        private GameObject planetPrefab;

        [SerializeField, Tooltip("Configuração geral do jogo")]
        private GameConfig gameConfig;

        [SerializeField, Tooltip("Centro do universo em torno do qual os planetas orbitam")]
        private Transform universeCenter;

        private readonly List<Planets> _activePlanets = new List<Planets>();
        private PlanetResourceManager _resourceManager;
        private PlanetPool _planetPool;

        private void Awake()
        {
            _resourceManager = GetComponent<PlanetResourceManager>();
            _planetPool = new PlanetPool(planetPrefab, gameConfig.numPlanets);

            if (universeCenter == null)
            {
                Debug.LogError("UniverseCenter não atribuído no PlanetSpawner. Os planetas não orbitarão.");
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
                Debug.LogError("GameConfig não está definido no PlanetSpawner.");
                return;
            }

            // Valida o número de planetas
            int minPlanets = 2 * Enum.GetValues(typeof(PlanetResources)).Length;
            if (gameConfig.numPlanets < minPlanets)
            {
                Debug.LogWarning($"O número de planetas ({gameConfig.numPlanets}) é menor que o mínimo exigido ({minPlanets}). Ajustando automaticamente.");
                gameConfig.numPlanets = minPlanets;
            }

            ClearActivePlanets();

            // Gera a lista de recursos
            var resources = _resourceManager.GenerateResourceList(gameConfig.numPlanets);

            // Calcula as órbitas
            float currentRadius = gameConfig.minOrbitRadius;
            List<(Vector3 position, PlanetData data)> planetPositions = new List<(Vector3, PlanetData)>();

            for (int i = 0; i < gameConfig.numPlanets; i++)
            {
                PlanetData planetData = _resourceManager.GetRandomPlanetData();
                if (planetData == null)
                {
                    Debug.LogError($"Nenhum PlanetData válido retornado para o planeta {i + 1}.");
                    continue;
                }

                // Define a posição na órbita (ângulo aleatório no plano XZ, Y fixo)
                float angle = Random.Range(0f, 360f);
                Vector3 position = new Vector3(
                    universeCenter.position.x + Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius,
                    universeCenter.position.y,
                    universeCenter.position.z + Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius
                );

                planetPositions.Add((position, planetData));

                // Atualiza o raio para a próxima órbita (tamanho no plano XZ + margem)
                float spacing = planetData.size + gameConfig.orbitMargin;
                currentRadius += spacing;

                // Log para depuração
                Debug.Log($"Planeta {i + 1}: Raio={currentRadius:F2}, Tamanho XZ={planetData.size:F2}, Margem={gameConfig.orbitMargin:F2}, Posição={position}, Escala Máxima={planetData.maxScaleMultiplier:F2}");
            }

            // Spawna os planetas nas posições calculadas
            for (int i = 0; i < planetPositions.Count; i++)
            {
                var (position, planetData) = planetPositions[i];
                GameObject planetObj = _planetPool.GetPlanet(position);
                planetObj.name = $"Planeta_{i + 1}";

                Planets planet = planetObj.GetComponent<Planets>();
                planet.SetPlanetData(planetData);
                planet.SetResource(resources[i]);
                planet.Initialize();
                planet.OnDeath += OnPlanetDestroyed;
                if (universeCenter != null)
                {
                    planet.StartOrbit(universeCenter);
                }
                _activePlanets.Add(planet);

                Debug.Log($"Planeta {i + 1} spawnado na posição: {position} com recurso: {resources[i]}");
            }
        }

        private void ClearActivePlanets()
        {
            foreach (var planet in _activePlanets)
            {
                planet.OnDeath -= OnPlanetDestroyed;
                _planetPool.ReturnPlanet(planet.gameObject);
            }
            _activePlanets.Clear();
        }

        private void OnPlanetDestroyed(DestructibleObject planet)
        {
            _activePlanets.Remove(planet as Planets);
            _planetPool.ReturnPlanet(planet.gameObject);
        }

        public List<Planets> GetActivePlanets() => _activePlanets;
    }
}