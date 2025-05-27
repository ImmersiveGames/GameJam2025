using System.Collections.Generic;
using _ImmersiveGames.Scripts.PoolSystemOld;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystemsOLD
{
    public class PlanetSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform universeCenter;
        [SerializeField] private PlanetResourceManager planetResourceManager;
        [SerializeField] private PlanetObjectPool planetPool;
        [SerializeField, Tooltip("Ativar logs para depuração")]
        private bool debugMode;

        private readonly List<Planets> _spawnedPlanets = new List<Planets>();

        private void Awake()
        {
            if (!gameConfig)
            {
                DebugUtility.LogError<PlanetSpawner>("GameConfig não configurado.", this);
                enabled = false;
            }
            if (!universeCenter)
            {
                DebugUtility.LogError<PlanetSpawner>("UniverseCenter não configurado.", this);
                enabled = false;
            }
            if (!planetResourceManager)
            {
                DebugUtility.LogError<PlanetSpawner>("PlanetResourceManager não configurado.", this);
                enabled = false;
            }
            if (!planetPool)
            {
                DebugUtility.LogError<PlanetSpawner>("PlanetObjectPool não configurado.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.EventStartGame += HandleGameStarted;
            }
        }

        private void OnDisable()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.EventStartGame -= HandleGameStarted;
            }
        }

        private void HandleGameStarted()
        {
            _spawnedPlanets.Clear();
            SpawnPlanets();
        }

        private void SpawnPlanets()
        {
            var planetResources = planetResourceManager.GenerateResourceList(gameConfig.numPlanets);
            float currentRadius = gameConfig.minOrbitRadius;
            for (var i = 0; i < gameConfig.numPlanets; i++)
            {
                var planetData = planetResourceManager.GetRandomPlanetData();
                if (planetData == null) continue;

                var planetObj = planetPool.GetObject(Vector3.zero, Quaternion.identity, gameConfig.numPlanets);
                if (planetObj == null) continue;

                planetObj.transform.SetParent(universeCenter, false);

                var planet = planetObj.GetComponent<Planets>();
                planet.SetPlanetData(planetData);
                planet.SetResource(planetResources[i]);

                float angle = Random.Range(0f, 360f);
                Vector3 orbitPosition = universeCenter.position + Quaternion.Euler(0, angle, 0) * new Vector3(currentRadius, 0, 0);
                planetObj.transform.position = new Vector3(orbitPosition.x, universeCenter.position.y, orbitPosition.z);

                planet.StartOrbit(universeCenter);
                //currentRadius += planetData.size + gameConfig.orbitMargin;

                planet.Initialize();
                _spawnedPlanets.Add(planet);

                if (debugMode)
                {
                    DebugUtility.LogVerbose<PlanetSpawner>($"Planeta {planetObj.name} spawnado em {planetObj.transform.position} (Y do universeCenter: {universeCenter.position.y}), recurso: {planetResources[i]}", "blue", this);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var planet in _spawnedPlanets)
            {
                if (planet != null)
                {
                    var pooledObj = planet.GetComponent<PooledObject>();
                    if (pooledObj != null)
                    {
                        pooledObj.ReturnSelfToPool();
                    }
                    else
                    {
                        planet.gameObject.SetActive(false);
                    }
                }
            }
            _spawnedPlanets.Clear();
        }
    }
}