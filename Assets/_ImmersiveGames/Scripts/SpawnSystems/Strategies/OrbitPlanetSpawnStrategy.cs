using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "OrbitPlanetSpawnStrategy", menuName = "ImmersiveGames/Strategies/OrbitPlanetSpawn")]
    [DebugLevel(DebugLevel.Warning)]
    public class OrbitPlanetSpawnStrategy : SpawnStrategySo
    {
        // Constantes para evitar valores mágicos
        private const float MinAngleSeparationDegrees = 10f;
        private const float AngleVariationDegrees = 10f;
        private const int MaxAngleAttempts = 50;
        
        [Header("Spawn Settings")]
        [SerializeField] private bool useRandomAngles = false;
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

        private void SpawnPlanets(IPoolable[] objects, PlanetSpawnData planetSpawnData)
        {
            float currentRadius = planetSpawnData.InitialOrbitRadius;
            float lastScaledDiameter = 0f;
            var usedAngles = new List<float>();
            
            List<PlanetResourcesSo> resourceList = _planetsManager.GenerateResourceList(objects.Length, planetSpawnData.PlanetResources);

            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] == null)
                {
                    DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>($"Objeto {index} é nulo!");
                    continue;
                }

                var planetInfo = GetRandomPlanetData(planetSpawnData.PlanetOptions);
                if (!planetInfo)
                {
                    DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>("Nenhum PlanetData válido encontrado!");
                    continue;
                }

                if (!ProcessPlanet(objects[index], planetInfo, index, planetSpawnData, resourceList[index], 
                    ref currentRadius, ref lastScaledDiameter, usedAngles, objects.Length))
                {
                    continue;
                }
            }
        }

        private bool ProcessPlanet(IPoolable poolableObject, PlanetData planetInfo, int index, 
            PlanetSpawnData spawnData, PlanetResourcesSo planetResource, 
            ref float currentRadius, ref float lastScaledDiameter, List<float> usedAngles, int totalPlanets)
        {
            int scaleMult = Random.Range(planetInfo.minScale, planetInfo.maxScale);
            float scaledDiameter = planetInfo.size * scaleMult;

            var planetGo = poolableObject.GetGameObject();
            if (planetGo == null)
            {
                DebugUtility.LogWarning<OrbitPlanetSpawnStrategy>($"GameObject do planeta {index} é nulo!");
                return false;
            }

            currentRadius = CalculateOrbitRadius(lastScaledDiameter, scaledDiameter, currentRadius, spawnData.SpaceBetweenPlanets);
            float initialAngle = GetPlanetAngle(index, totalPlanets, usedAngles);
            usedAngles.Add(initialAngle);

            PositionPlanet(poolableObject, planetGo, currentRadius, initialAngle, spawnData.OrbitCenter);
            _planetsManager.ConfigurePlanet(planetGo, planetInfo, index, planetResource, currentRadius, scaleMult, initialAngle);

            lastScaledDiameter = scaledDiameter;
            
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Planeta {index} spawnado - Raio: {currentRadius:F2}, Ângulo: {initialAngle * Mathf.Rad2Deg:F1}°, Diâmetro: {scaledDiameter:F2}");
            
            return true;
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

        private void PositionPlanet(IPoolable poolableObject, GameObject planetGo, float radius, float angle, Vector3 orbitCenter)
        {
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            var spawnPos = orbitCenter + offset;
            
            planetGo.transform.position = spawnPos;
            poolableObject.Activate(spawnPos);
            
            DebugUtility.Log<OrbitPlanetSpawnStrategy>(
                $"Planeta posicionado em {spawnPos} (raio: {radius:F2}, ângulo: {angle * Mathf.Rad2Deg:F1}°)");
        }

        private float GetPlanetAngle(int planetIndex, int totalPlanets, List<float> usedAngles)
        {
            if (useRandomAngles)
            {
                return GetRandomAngleWithValidation(usedAngles, totalPlanets);
            }
            else
            {
                return GetOptimalAngle(planetIndex, totalPlanets);
            }
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
            baseAngle = baseAngle % 360f;
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

        #region Editor Utilities
        
        #if UNITY_EDITOR
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private string debugInfo = "Configure as opções acima";
        
        private void OnValidate()
        {
            debugInfo = $"Modo: {(useRandomAngles ? "Aleatório" : "Equidistante")}, " +
                       $"Variação: {(addAngleVariation ? "Sim" : "Não")}";
        }
        #endif
        
        #endregion
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