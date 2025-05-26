using System.Collections.Generic;
using _ImmersiveGames.Scripts.Game.Planets;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetConfig planetConfig;
        [SerializeField] private Transform universeCenter;

        private readonly List<Planet> _spawnedPlanets = new List<Planet>();
        private ISpawnStrategy _spawnStrategy;
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;

        private void Awake()
        {
            if (!planetConfig || planetConfig.PlanetDatas.Count == 0)
            {
                Debug.LogError($"PlanetConfig não configurado ou vazio em {name}.", this);
                enabled = false;
                return;
            }
            if (!universeCenter)
            {
                Debug.LogError($"UniverseCenter não configurado em {name}.", this);
                enabled = false;
                return;
            }

            _spawnStrategy = new OrbitSpawnStrategy();
            _planetDestroyedBinding = new EventBinding<PlanetDestroyedEvent>(HandlePlanetDestroyed);

            // Registra pools sem configuração
            foreach (var planetData in planetConfig.PlanetDatas)
            {
                if (!PoolManager.Instance.GetPool(planetData.PoolableData.ObjectName))
                {
                    PoolManager.Instance.RegisterPool(planetData.PoolableData);
                }
            }
        }

        private void OnEnable()
        {
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager)
            {
                gameManager.EventStartGame += HandleGameStarted;
            }
        }

        private void OnDisable()
        {
            EventBus<PlanetDestroyedEvent>.Unregister(_planetDestroyedBinding);
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager)
            {
                gameManager.EventStartGame -= HandleGameStarted;
            }
        }

        private void HandleGameStarted()
        {
            ResetPlanets();
            SpawnPlanets();
        }

        private void SpawnPlanets()
        {
            float currentRadius = planetConfig.MinOrbitRadius;
            for (int i = 0; i < planetConfig.NumPlanets; i++)
            {
                var planetData = planetConfig.PlanetDatas[Random.Range(0, planetConfig.PlanetDatas.Count)];
                var poolKey = planetData.PoolableData.ObjectName;

                var planetObj = PoolManager.Instance.GetObject(poolKey, Vector3.zero).GetGameObject();
                if (!planetObj)
                {
                    Debug.LogError($"Objeto do pool '{poolKey}' não encontrado. Verifique se o pool foi registrado corretamente.", this);
                    continue;
                }

                planetObj.transform.SetParent(universeCenter, false);
                var planet = planetObj.GetComponent<Planet>();
                if (!planet)
                {
                    Debug.LogError($"Componente Planet não encontrado em {planetObj.name}.", planetObj);
                    planetObj.GetComponent<PooledObject>()?.ReturnToPool();
                    continue;
                }

                float angle = Random.Range(0f, 360f);
                Vector3 orbitPosition = universeCenter.position + Quaternion.Euler(0, angle, 0) * new Vector3(currentRadius, 0, 0);
                var objects = new[] { planetObj.GetComponent<IPoolable>() };
                _spawnStrategy.Spawn(objects, orbitPosition, planetData, universeCenter.position);

                // Configura planeta no spawn
                float orbitSpeed = planetData.ReconfigureOnSpawn ? Random.Range(planetData.MinOrbitSpeed, planetData.MaxOrbitSpeed) : planetData.MinOrbitSpeed;
                bool orbitClockwise = planetData.ReconfigureOnSpawn ? (Random.value > 0.5f) : planetData.OrbitClockwise;
                planet.Initialize(planetData, universeCenter, orbitSpeed, orbitClockwise);

                _spawnedPlanets.Add(planet);
                currentRadius += planetData.Size + planetConfig.OrbitMargin;
            }
        }

        private void HandlePlanetDestroyed(PlanetDestroyedEvent evt)
        {
            _spawnedPlanets.Remove(evt.Planet);
            if (_spawnedPlanets.Count == 0)
            {
                EventBus<GameOverEvent>.Raise(new GameOverEvent());
            }
        }

        public void ResetPlanets()
        {
            foreach (var planet in _spawnedPlanets)
            {
                if (planet)
                {
                    planet.ResetState();
                    planet.GetComponent<PooledObject>()?.ReturnToPool();
                }
            }
            _spawnedPlanets.Clear();
            SpawnPlanets(); // Respawn com novas configurações
        }
    }
}