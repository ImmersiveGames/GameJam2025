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
        [SerializeField] private Vector3 orbitCenter = Vector3.zero; // Centro com Y = 0

        public override void Spawn(IPoolable[] objects, SpawnData data,Vector3 origin, Vector3 forward)
        {
            var resourceList = GenerateResourceList(objects.Length);
            float currentRadius = initialOrbitRadius;
            float lastDiameter = 0f;

            // Lista para armazenar ângulos usados e evitar sobreposição
            List<float> usedAngles = new List<float>();

            for (int index = 0; index < objects.Length; index++)
            {
                var obj = objects[index];
                if (obj == null) continue;

                var planetGo = obj.GetGameObject();
                var planetInfo = GetRandomPlanetData();
                if (planetInfo == null) continue;

                // Cálculo do raio da órbita
                float diameter = planetInfo.size;
                currentRadius += (lastDiameter / 2f) + (diameter / 2f) + spaceBetweenPlanets;

                // Escolher um ângulo aleatório, evitando sobreposição
                float angle = GetUniqueRandomAngle(usedAngles);
                usedAngles.Add(angle);

                // Calcular posição inicial no perímetro da órbita
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * currentRadius;
                Vector3 spawnPos = orbitCenter + offset; // Posição no plano XZ

                planetGo.transform.position = spawnPos;
                obj.Activate(spawnPos);

                lastDiameter = diameter;

                // Configuração do planeta
                planetGo.name = $"Planet_{planetInfo.name}_{index}";
                planetGo.transform.localPosition = Vector3.zero;
                int scaleMult = Random.Range(planetInfo.minScale, planetInfo.maxScale);
                planetGo.transform.localScale = Vector3.one * scaleMult;
                float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
                planetGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);

                // Configuração do PlanetMotion
                PlanetMotion motion = planetGo.GetComponent<PlanetMotion>();
                if (!motion)
                    motion = planetGo.AddComponent<PlanetMotion>();
                bool randomOrbit = Random.value > 0.5f;
                bool randomRotate = Random.value > 0.5f;
                motion.Initialize(
                    center: orbitCenter,
                    radius: currentRadius,
                    orbitSpeedDegPerSec: Random.Range(planetInfo.minOrbitSpeed, planetInfo.maxOrbitSpeed),
                    orbitClockwise: randomOrbit,
                    selfRotationSpeedDegPerSec: Random.Range(planetInfo.minRotationSpeed, planetInfo.maxRotationSpeed) * (randomRotate ? -1f : 1f)
                );

                // Configuração do Planets
                Planets planets = planetGo.GetComponent<Planets>();
                if (!planets) continue;
                planets.Initialize(index, planetInfo, resourceList[index]);
            }
        }

        private float GetUniqueRandomAngle(List<float> usedAngles)
        {
            const float minAngleSeparation = 30f; // Mínima separação angular para evitar planetas muito próximos
            float angle;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                attempts++;
                bool isValid = true;

                foreach (float usedAngle in usedAngles)
                {
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, usedAngle * Mathf.Rad2Deg));
                    if (angleDiff < minAngleSeparation)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid || attempts >= maxAttempts)
                    return angle;
            } while (true);
        }

        private PlanetData GetRandomPlanetData()
        {
            if (planetOptions == null || planetOptions.Count == 0)
                return null;
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }

        private List<PlanetResourcesSo> GenerateResourceList(int numPlanets)
        {
            var resourceDistribution = new List<PlanetResourcesSo>(planetResources);
            while (resourceDistribution.Count < numPlanets)
            {
                resourceDistribution.Add(planetResources[Random.Range(0, planetResources.Count)]);
            }

            for (int i = resourceDistribution.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (resourceDistribution[i], resourceDistribution[j]) = (resourceDistribution[j], resourceDistribution[i]);
            }

            return resourceDistribution;
        }
    }
}