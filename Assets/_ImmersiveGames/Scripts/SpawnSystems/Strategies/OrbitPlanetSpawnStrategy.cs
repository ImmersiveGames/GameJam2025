using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy", menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (data is not PlanetSpawnData planetSpawnData)
            {
                Debug.LogError("SpawnData não é do tipo PlanetSpawnData!");
                return;
            }

            if (!ValidateInputs(objects, planetSpawnData)) return;

            float currentRadius = planetSpawnData.InitialOrbitRadius;
            float lastScaledDiameter = 0f;
            var usedAngles = new List<float>();
            List<PlanetResourcesSo> resourceList = PlanetsManager.Instance.GenerateResourceList(objects.Length, planetSpawnData.PlanetResources);

            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] == null)
                {
                    Debug.LogWarning($"Objeto {index} é nulo!");
                    continue;
                }

                var planetInfo = GetRandomPlanetData(planetSpawnData.PlanetOptions);
                if (!planetInfo)
                {
                    Debug.LogWarning("Nenhum PlanetData válido encontrado!");
                    continue;
                }

                int scaleMult = Random.Range(planetInfo.minScale, planetInfo.maxScale);
                float scaledDiameter = planetInfo.size * scaleMult;

                var planetGo = objects[index].GetGameObject();
                currentRadius = CalculateOrbitRadius(lastScaledDiameter, scaledDiameter, currentRadius, planetSpawnData.SpaceBetweenPlanets);
                float initialAngle = GetUniqueRandomAngle(usedAngles, objects.Length); // Ângulo inicial em radianos
                usedAngles.Add(initialAngle);

                PositionPlanet(objects[index], planetGo, currentRadius, initialAngle, planetSpawnData.OrbitCenter);

                // Passar o ângulo inicial para ConfigurePlanet
                PlanetsManager.Instance.ConfigurePlanet(planetGo, planetInfo, index, resourceList[index], currentRadius, scaleMult, initialAngle);

                lastScaledDiameter = scaledDiameter;
                Debug.Log($"Planeta {index} spawnado em raio {currentRadius}, ângulo inicial {initialAngle * Mathf.Rad2Deg} graus, diâmetro escalado {scaledDiameter}.");
            }
        }

        private bool ValidateInputs(IPoolable[] objects, PlanetSpawnData spawnData)
        {
            if (objects == null || objects.Length == 0)
            {
                Debug.LogWarning("Lista de objetos vazia ou nula!");
                return false;
            }
            if (spawnData.PlanetOptions == null || spawnData.PlanetOptions.Count == 0)
            {
                Debug.LogWarning("Nenhuma opção de planeta configurada!");
                return false;
            }
            if (spawnData.PlanetResources == null || spawnData.PlanetResources.Count == 0)
            {
                Debug.LogWarning("Nenhum recurso configurado!");
                return false;
            }
            return true;
        }

        private float CalculateOrbitRadius(float lastScaledDiameter, float currentScaledDiameter, float currentRadius, float spaceBetweenPlanets)
        {
            float newRadius = currentRadius + (lastScaledDiameter / 2f) + (currentScaledDiameter / 2f) + spaceBetweenPlanets;
            Debug.Log($"Calculado novo raio: {newRadius} (lastScaledDiameter: {lastScaledDiameter}, currentScaledDiameter: {currentScaledDiameter}, spaceBetween: {spaceBetweenPlanets})");
            return newRadius;
        }

        private void PositionPlanet(IPoolable obj, GameObject planetGo, float radius, float angle, Vector3 orbitCenter)
        {
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Vector3 spawnPos = orbitCenter + offset;
            planetGo.transform.position = spawnPos;
            obj.Activate(spawnPos);
            Debug.Log($"Planeta posicionado em {spawnPos}, raio {radius}, ângulo {angle * Mathf.Rad2Deg} graus.");
        }

        private float GetUniqueRandomAngle(List<float> usedAngles, int totalPlanets)
        {
            float minAngleSeparation = Mathf.Max(360f / Mathf.Max(totalPlanets, 1), 15f);
            const int maxAttempts = 100;
            int attempts = 0;
            while (attempts < maxAttempts)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
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
                if (isValid)
                {
                    Debug.Log($"Ângulo válido encontrado: {angle * Mathf.Rad2Deg} graus após {attempts} tentativas.");
                    return angle;
                }
                attempts++;
            }
            float fallbackAngle = (360f / Mathf.Max(totalPlanets, 1)) * usedAngles.Count * Mathf.Deg2Rad;
            Debug.LogWarning($"Fallback: Usando ângulo equidistante {fallbackAngle * Mathf.Rad2Deg} graus após {maxAttempts} tentativas.");
            return fallbackAngle;
        }

        private PlanetData GetRandomPlanetData(List<PlanetData> planetOptions)
        {
            if (planetOptions == null || planetOptions.Count == 0) return null;
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }
    }
}