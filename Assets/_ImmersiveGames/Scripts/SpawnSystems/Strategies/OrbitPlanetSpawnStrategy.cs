using System.Collections.Generic;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using PlanetData = _ImmersiveGames.Scripts.PlanetSystems.PlanetData;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy", menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private List<PlanetData> planetOptions;
        [SerializeField] private List<PlanetResourcesSo> planetResources;
        [SerializeField] private float spaceBetweenPlanets = 2f;
        [SerializeField] private float initialOrbitRadius = 10f;
        [SerializeField] private Vector3 orbitCenter;
        public override void Spawn(IPoolable[] objects, Vector3 origin, Vector3 forward)
        {
            var resourceList = GenerateResourceList(objects.Length);
            float currentRadius = initialOrbitRadius;
            float lastDiameter = 0f;

            for (int index = 0; index < objects.Length; index++)
            {
                var obj = objects[index];
                if (obj == null) continue;

                var planetGo = obj.GetGameObject();

                // Seleciona um PlanetData aleatório
                var planetInfo = GetRandomPlanetData();
                if (planetInfo == null) continue;

                // === CÁLCULO DE DIÂMETRO E POSIÇÃO ORBITAL ===
                float diameter = planetInfo.size;
                currentRadius += (lastDiameter / 2f) + (diameter / 2f) + spaceBetweenPlanets;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * currentRadius;
                Vector3 spawnPos = origin + offset;

                planetGo.transform.position = spawnPos;
                obj.Activate(spawnPos);

                lastDiameter = diameter;

                //-- === CONFIGURAÇÃO DO PLANET GAMEOBJECT ===
                planetGo.name = $"Planet_{planetInfo.name}_{index}";
                planetGo.transform.localPosition = Vector3.zero;
                // Escala aleatória
                int scaleMult = Random.Range(planetInfo.minScale, planetInfo.maxScale);
                planetGo.transform.localScale = Vector3.one * scaleMult;
                float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
                planetGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);


                // === CONFIGURAÇÃO DO PLANET MOTION ===
                PlanetMotion motion = planetGo.GetComponent<PlanetMotion>();
                if (!motion)
                    motion = planetGo.AddComponent<PlanetMotion>();
                bool randomOrbit = Random.value > 0.5f;
                bool randomRotate = Random.value > 0.5f;
                // Injeta os valores do PlanetData no PlanetMotion
                motion.Initialize(
                    center: orbitCenter,
                    radius: currentRadius,
                    orbitSpeedDegPerSec: Random.Range(planetInfo.minOrbitSpeed, planetInfo.maxOrbitSpeed),
                    orbitClockwise: randomOrbit,
                    selfRotationSpeedDegPerSec: Random.Range(planetInfo.minRotationSpeed, planetInfo.maxRotationSpeed) *
                    (randomRotate ? -1f : 1f)
                );
                // === CONFIGURAÇÃO DO PLANET Resorces ===
                Planets planets = planetGo.GetComponent<Planets>();
                if (!planets) return;
                planets.OnEventPlanetCreated(index, planetInfo, resourceList[index]);
            }
        }
        private PlanetData GetRandomPlanetData()
        {
            if (planetOptions == null || planetOptions.Count == 0)
                return null;
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }
        
        private List<PlanetResourcesSo> GenerateResourceList(int numPlanets)
        {
            var resourceDistribution = new List<PlanetResourcesSo>(planetResources); // Garante um de cada recurso
            while (resourceDistribution.Count < numPlanets)
            {
                resourceDistribution.Add(planetResources[Random.Range(0, planetResources.Count)]);
            }

            // Embaralhamento Fisher-Yates
            for (int i = resourceDistribution.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (resourceDistribution[i], resourceDistribution[j]) = (resourceDistribution[j], resourceDistribution[i]);
            }

            return resourceDistribution;
        }

    }
}