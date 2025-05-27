using System.Collections.Generic;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy",menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private List<PlanetData> planetOptions;
        [SerializeField] private float spaceBetweenPlanets = 2f;
        [SerializeField] private float initialOrbitRadius = 10f;
        [SerializeField] private Vector3 orbitCenter;
        public override void Spawn(IPoolable[] objects, Vector3 origin, Vector3 forward)
        {
            
            float currentRadius = initialOrbitRadius;
            float lastDiameter = 0f;

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                GameObject planetGO = obj.GetGameObject();

                // Seleciona um PlanetData aleatório
                var planetInfo = GetRandomPlanetData();
                if (planetInfo == null) continue;

                // === CÁLCULO DE DIÂMETRO E POSIÇÃO ORBITAL ===
                float diameter = planetInfo.size;
                currentRadius += (lastDiameter / 2f) + (diameter / 2f) + spaceBetweenPlanets;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * currentRadius;
                Vector3 spawnPos = origin + offset;

                planetGO.transform.position = spawnPos;
                obj.Activate(spawnPos);

                lastDiameter = diameter;

                // === PLANET MODEL INSTANTIATION ===
                if (planetInfo.ModelPrefab)
                {
                    GameObject model = Instantiate(planetInfo.ModelPrefab, planetGO.transform);
                    model.transform.localPosition = Vector3.zero;

                    // Escala aleatória
                    float scaleMult = Random.Range(planetInfo.minScaleMultiplier, planetInfo.maxScaleMultiplier);
                    model.transform.localScale = Vector3.one * scaleMult;

                    // Inclinação aleatória
                    float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
                    model.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                }

                // === CONFIGURAÇÃO DO PLANET MOTION ===
                PlanetMotion motion = planetGO.GetComponent<PlanetMotion>();
                if (motion == null)
                    motion = planetGO.AddComponent<PlanetMotion>();

// Injeta os valores do PlanetData no PlanetMotion
                motion.Initialize(
                    center: orbitCenter,
                    radius: currentRadius,
                    orbitSpeedDegPerSec: Random.Range(planetInfo.minOrbitSpeed, planetInfo.maxOrbitSpeed),
                    orbitClockwise: planetInfo.orbitClockwise,
                    selfRotationSpeedDegPerSec: Random.Range(planetInfo.minRotationSpeed, planetInfo.maxRotationSpeed) *
                    (planetInfo.rotateClockwise ? -1f : 1f)
                );
            }
        }
        public PlanetData GetRandomPlanetData()
        {
            if (planetOptions == null || planetOptions.Count == 0)
                return null;
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }
        
    }
}