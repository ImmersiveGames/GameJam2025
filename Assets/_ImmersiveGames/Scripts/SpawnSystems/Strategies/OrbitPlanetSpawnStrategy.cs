using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.ResourceSystems;
using PlanetData = _ImmersiveGames.Scripts.PlanetSystems.PlanetData;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    // Estratégia de spawn para posicionar planetas em órbitas circulares
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy", menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private List<PlanetData> planetOptions; // Opções de dados de planetas
        [SerializeField] private List<PlanetResourcesSo> planetResources; // Recursos disponíveis
        [SerializeField] private float spaceBetweenPlanets = 2f; // Espaço mínimo entre planetas
        [SerializeField] private float initialOrbitRadius = 10f; // Raio inicial da órbita
        [SerializeField] private Vector3 orbitCenter = Vector3.zero; // Centro da órbita (Y = 0)

        // Executa o spawn dos planetas
        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (!ValidateInputs(objects)) return;

            var resourceList = GenerateResourceList(objects.Length);
            float currentRadius = initialOrbitRadius;
            float lastDiameter = 0f;
            var usedAngles = new List<float>();

            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] == null) continue;

                var planetInfo = GetRandomPlanetData();
                if (planetInfo == null) continue;

                var planetGo = objects[index].GetGameObject();
                currentRadius = CalculateOrbitRadius(lastDiameter, planetInfo.size, currentRadius);
                float angle = GetUniqueRandomAngle(usedAngles, objects.Length);
                usedAngles.Add(angle);

                PositionPlanet(objects[index], planetGo, currentRadius, angle);
                ConfigurePlanetMotion(planetGo, planetInfo, currentRadius);
                ConfigurePlanetProperties(planetGo, planetInfo, index, resourceList[index]);

                lastDiameter = planetInfo.size;
            }
        }

        // Valida entradas para evitar erros
        private bool ValidateInputs(IPoolable[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                Debug.LogWarning("Lista de objetos vazia ou nula!");
                return false;
            }
            if (planetOptions == null || planetOptions.Count == 0)
            {
                Debug.LogWarning("Nenhuma opção de planeta configurada!");
                return false;
            }
            if (planetResources == null || planetResources.Count == 0)
            {
                Debug.LogWarning("Nenhum recurso configurado!");
                return false;
            }
            return true;
        }

        // Calcula o raio da órbita com base no diâmetro anterior e atual
        private float CalculateOrbitRadius(float lastDiameter, float currentDiameter, float currentRadius)
        {
            return currentRadius + (lastDiameter / 2f) + (currentDiameter / 2f) + spaceBetweenPlanets;
        }

        // Posiciona o planeta na órbita
        private void PositionPlanet(IPoolable obj, GameObject planetGo, float radius, float angle)
        {
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Vector3 spawnPos = orbitCenter + offset; // Posição no plano XZ
            planetGo.transform.position = spawnPos;
            obj.Activate(spawnPos);
        }

        // Configura o movimento do planeta
        private void ConfigurePlanetMotion(GameObject planetGo, PlanetData planetInfo, float radius)
        {
            var motion = planetGo.GetComponent<PlanetMotion>() ?? planetGo.AddComponent<PlanetMotion>();
            bool randomOrbit = Random.value > 0.5f;
            bool randomRotate = Random.value > 0.5f;
            motion.Initialize(
                center: orbitCenter,
                radius: radius,
                orbitSpeedDegPerSec: Random.Range(planetInfo.minOrbitSpeed, planetInfo.maxOrbitSpeed),
                orbitClockwise: randomOrbit,
                selfRotationSpeedDegPerSec: Random.Range(planetInfo.minRotationSpeed, planetInfo.maxRotationSpeed) * (randomRotate ? -1f : 1f)
            );
        }

        // Configura propriedades do planeta (nome, escala, inclinação, recursos)
        private void ConfigurePlanetProperties(GameObject planetGo, PlanetData planetInfo, int index, PlanetResourcesSo resource)
        {
            planetGo.name = $"Planet_{planetInfo.name}_{index}";
            planetGo.transform.localPosition = Vector3.zero;
            int scaleMult = Random.Range(planetInfo.minScale, planetInfo.maxScale);
            planetGo.transform.localScale = Vector3.one * scaleMult;
            float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
            planetGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);

            
            
            
            var planets = planetGo.GetComponent<Planets>();
            if (planets != null)
            {
                planets.Initialize(index, planetInfo, resource);
                // Reinicia recursos associados (ex.: HealthResource)
                var healthResource = planetGo.GetComponent<HealthResource>();
                if (healthResource != null && healthResource is IResettable resettable)
                {
                    resettable.Reset();
                }
            }
            else
            {
                Debug.LogWarning($"Componente Planets não encontrado em {planetGo.name}!");
            }
        }

        // Gera um ângulo único para evitar sobreposição
        private float GetUniqueRandomAngle(List<float> usedAngles, int totalPlanets)
        {
            float minAngleSeparation = Mathf.Max(360f / (totalPlanets * 2f), 5f); // Separação mínima
            const int maxAttempts = 50; // Tentativas máximas
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
                    return angle;
                attempts++;
            }
            Debug.LogWarning($"Não foi possível encontrar ângulo ideal após {maxAttempts} tentativas! Total de planetas: {totalPlanets}, ângulos usados: {usedAngles.Count}, separação mínima: {minAngleSeparation} graus.", this);
            return Random.Range(0f, 360f) * Mathf.Deg2Rad; // Fallback inseguro
        }

        // Seleciona dados de planeta aleatoriamente
        private PlanetData GetRandomPlanetData()
        {
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }

        // Gera lista de recursos para os planetas
        private List<PlanetResourcesSo> GenerateResourceList(int numPlanets)
        {
            var resourceList = new List<PlanetResourcesSo>();
            for (int i = 0; i < numPlanets; i++)
            {
                resourceList.Add(planetResources[Random.Range(0, planetResources.Count)]);
            }
            return resourceList.OrderBy(_ => Random.value).ToList(); // Embaralha a lista
        }
    }
}