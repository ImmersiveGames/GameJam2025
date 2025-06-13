using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy", menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    [DebugLevel(DebugLevel.Verbose)]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        // Constantes para evitar valores mágicos
        private const float MinAngleSeparationDegrees = 10f;
        private const float AngleVariationDegrees = 10f;
        private const int MaxAngleAttempts = 50;
        
        [Header("Spawn Settings")]
        [SerializeField] private bool useRandomAngles;
        [SerializeField] private bool addAngleVariation = true;
        
        // Cache do PlanetsManager para performance
        private PlanetsManager _planetsManager;

        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (data is not PlanetSpawnData planetSpawnData)
            {
                DebugUtility.LogError<OrbitPlanetSpawnStrategy>("SpawnData não é do tipo PlanetSpawnData!");
                return;
            }
            _planetsManager = PlanetsManager.Instance;

            if (!ValidateInputs(objects, planetSpawnData)) return;

            SpawnPlanets(objects, planetSpawnData);
        }

        //A Criação dos planetas acontece todas aqui
        private void SpawnPlanets(IPoolable[] objects, PlanetSpawnData planetSpawnData)
        {
            List<PlanetResourcesSo> resourceList = _planetsManager.GenerateResourceList(objects.Length, planetSpawnData.PlanetResources);
            float currentOrbitRadius = planetSpawnData.InitialOrbitRadius;
            float lastScaledDiameter = 0f;
            var usedAngles = new List<float>();
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] == null)
                {
                    DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>($"Objeto {index} é nulo!");
                    continue;
                }

                // Obtém dados do planeta aleatório
                var planetInfo = GetRandomPlanetData(planetSpawnData.PlanetOptions);
                if (!planetInfo)
                {
                    DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>("Nenhum PlanetData válido encontrado!");
                    continue;
                }
                var planet = _planetsManager.ConfigurePlanet(objects[index], planetInfo, index, resourceList[index]);
                if (planet == null)
                {
                    DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>($"Falha ao criar planeta {index}!");
                    continue;
                }
                
                PlantOrbitPositioning(planet.GetPlanetInfo(),planetSpawnData, ref currentOrbitRadius, ref lastScaledDiameter, usedAngles,index, objects.Length);
                EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(planet));
                //_planetsManager.ConfigurePlanet(planetGo, planetInfo, index, planetResource, currentRadius, scaleMult, initialAngle);
            }
        }

        private void PlantOrbitPositioning(PlanetsMaster.PlanetInfo planetInfo, PlanetSpawnData spawnData, ref float currentOrbitRadius, ref float lastScaledDiameter, List<float> usedAngles,int index,int totalPlanets)
        {
            int scaleMult = planetInfo.planetScale;
            float scaledDiameter = 5 * scaleMult;
            
            currentOrbitRadius = CalculateOrbitRadius(lastScaledDiameter, scaledDiameter, currentOrbitRadius, spawnData.SpaceBetweenPlanets);
            float initialAngle = GetPlanetAngle(index, totalPlanets, usedAngles);
            PositionPlanet(planetInfo, currentOrbitRadius, initialAngle, spawnData.OrbitCenter);
            lastScaledDiameter = scaledDiameter;
            planetInfo.planetRadius = currentOrbitRadius;
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Planeta {index} spawnado - Raio: {currentOrbitRadius:F2}, Ângulo: {initialAngle * Mathf.Rad2Deg:F1}°, Diâmetro: {scaledDiameter:F2}");
        }
        

        private bool ValidateInputs(IPoolable[] objects, PlanetSpawnData spawnData)
        {
            if (objects == null || objects.Length == 0)
            {
                DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>("Lista de objetos vazia ou nula!");
                return false;
            }
            
            if (spawnData.PlanetOptions == null || spawnData.PlanetOptions.Count == 0)
            {
                DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>("Nenhuma opção de planeta configurada!");
                return false;
            }
            
            if (spawnData.PlanetResources == null || spawnData.PlanetResources.Count == 0)
            {
                DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>("Nenhum recurso configurado!");
                return false;
            }
            
            if (_planetsManager == null)
            {
                DebugUtility.LogError<OrbitPlanetSpawnStrategy>("PlanetsManager.Instance é nulo!");
                return false;
            }
            
            return true;
        }

        private float CalculateOrbitRadius(float lastScaledDiameter, float currentScaledDiameter, float currentRadius, float spaceBetweenPlanets)
        {
            float newRadius = currentRadius + (lastScaledDiameter * 0.5f) + (currentScaledDiameter * 0.5f) + spaceBetweenPlanets;
            
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Novo raio calculado: {newRadius:F2} (anterior: {lastScaledDiameter:F2}, atual: {currentScaledDiameter:F2}, espaço: {spaceBetweenPlanets:F2})");
            
            return newRadius;
        }

        private void PositionPlanet(PlanetsMaster.PlanetInfo planetInfo, float radius, float angle, Vector3 orbitCenter)
        {
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            planetInfo.orbitPosition = orbitCenter + offset;
            planetInfo.SetPlanetRadius(radius);
            planetInfo.initialAngle = angle;
            planetInfo.PlanetObject.transform.position = planetInfo.orbitPosition;
            planetInfo.PoolableObject.Activate(planetInfo.orbitPosition);
            
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Planeta posicionado em {planetInfo.orbitPosition} (raio: {radius:F2}, ângulo: {angle * Mathf.Rad2Deg:F1}°)");
        }

        private float GetPlanetAngle(int planetIndex, int totalPlanets, List<float> usedAngles)
        {
            return useRandomAngles ? GetRandomAngleWithValidation(usedAngles, totalPlanets) : GetOptimalAngle(planetIndex, totalPlanets);
        }

        /// <summary>
        /// Calcula ângulo ótimo com distribuição equidistante e variação opcional
        /// </summary>
        private float GetOptimalAngle(int planetIndex, int totalPlanets)
        {
            // Distribuição equidistante base
            float baseAngle = (360f / totalPlanets) * planetIndex;
            
            // Adiciona pequena variação aleatória se habilitada
            if (addAngleVariation)
            {
                float variation = Random.Range(-AngleVariationDegrees, AngleVariationDegrees);
                baseAngle += variation;
            }
            
            // Normaliza o ângulo para o range [0, 360)
            baseAngle %= 360f;
            if (baseAngle < 0f) baseAngle += 360f;
            
            float finalAngle = baseAngle * Mathf.Deg2Rad;
            
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Ângulo ótimo calculado: {baseAngle:F1}° para planeta {planetIndex}/{totalPlanets}");
            
            return finalAngle;
        }

        /// <summary>
        /// Versão melhorada do algoritmo de ângulo aleatório com validação
        /// </summary>
        private float GetRandomAngleWithValidation(List<float> usedAngles, int totalPlanets)
        {
            if (usedAngles.Count == 0)
            {
                float firstAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                DebugUtility.Log<OrbitPlanetSpawnStrategy>($"Primeiro ângulo aleatório: {firstAngle * Mathf.Rad2Deg:F1}°");
                return firstAngle;
            }

            // Separação mínima mais realista
            float minAngleSeparation = Mathf.Max(360f / (totalPlanets * 1.5f), MinAngleSeparationDegrees);
            
            for (int attempts = 0; attempts < MaxAngleAttempts; attempts++)
            {
                float candidateAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                
                if (IsAngleValid(candidateAngle, usedAngles, minAngleSeparation))
                {
                    DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                        $"Ângulo aleatório válido encontrado: {candidateAngle * Mathf.Rad2Deg:F1}° (tentativa {attempts + 1})");
                    return candidateAngle;
                }
            }
            
            // Fallback melhorado: usa distribuição equidistante
            float fallbackAngle = GetEquidistantFallbackAngle(usedAngles.Count, totalPlanets);
            
            DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>(
                $"Usando ângulo fallback equidistante: {fallbackAngle * Mathf.Rad2Deg:F1}° após {MaxAngleAttempts} tentativas");
            
            return fallbackAngle;
        }

        private bool IsAngleValid(float candidateAngle, List<float> usedAngles, float minSeparation)
        {
            float candidateAngleDegrees = candidateAngle * Mathf.Rad2Deg;
            
            foreach (float usedAngle in usedAngles)
            {
                float usedAngleDegrees = usedAngle * Mathf.Rad2Deg;
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(candidateAngleDegrees, usedAngleDegrees));
                
                if (angleDifference < minSeparation)
                {
                    return false;
                }
            }
            
            return true;
        }

        private float GetEquidistantFallbackAngle(int planetIndex, int totalPlanets)
        {
            return (360f / totalPlanets) * planetIndex * Mathf.Deg2Rad;
        }

        private PlanetData GetRandomPlanetData(List<PlanetData> planetOptions)
        {
            if (planetOptions == null || planetOptions.Count == 0)
            {
                DebugUtility.LogError<OrbitPlanetSpawnStrategy>("Lista de opções de planetas é nula ou vazia!");
                return null;
            }
            
            return planetOptions[Random.Range(0, planetOptions.Count)];
        }
    }

    #if UNITY_EDITOR
    // Atributo para campos read-only no inspector
    public class ReadOnlyAttribute : PropertyAttribute { }
    
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif
}