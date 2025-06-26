using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class OrbitPlanetStrategy : ISpawnStrategy
    {
        // Constantes
        private const float FullCircleDegrees = 360f;
        private const float AngleSeparationFactor = 1.5f;
        private const int MaxCollisionAttempts = 3; // Reduzido para evitar iterações desnecessárias

        // Configurações
        private readonly float _minAngleSeparationDegrees;
        private readonly float _angleVariationDegrees;
        private readonly int _maxAngleAttempts;
        private readonly bool _useRandomAngles;
        private readonly bool _addAngleVariation;
        private readonly float _initialOffset;
        private readonly Vector3 _orbitCenter;
        private readonly float _spaceBetweenPlanets;
        private readonly int _maxPlanets;
        private readonly float _orbitSpeed;
        private readonly PlanetsManager _planetsManager;

        private readonly List<float> _orbitalRadii = new();
        private readonly OrbitGizmoDrawer _gizmoDrawer;

        public OrbitPlanetStrategy(EnhancedStrategyData data)
        {
            _minAngleSeparationDegrees = data.GetProperty("minAngleSeparationDegrees", 10f);
            _angleVariationDegrees = data.GetProperty("angleVariationDegrees", 10f);
            _maxAngleAttempts = data.GetProperty("maxAngleAttempts", 50);
            _useRandomAngles = data.GetProperty("useRandomAngles", false);
            _addAngleVariation = data.GetProperty("addAngleVariation", false);
            _initialOffset = data.GetProperty("initialOffset", 10f);
            _orbitCenter = data.GetProperty("orbitCenter", Vector3.zero);
            _spaceBetweenPlanets = data.GetProperty("spaceBetweenPlanets", 10f);
            _maxPlanets = data.GetProperty("maxPlanets", 10);
            _orbitSpeed = data.GetProperty("orbitSpeed", 10f);
            _planetsManager = PlanetsManager.Instance;

            if (_planetsManager == null)
            {
                DebugUtility.LogError<OrbitPlanetStrategy>("PlanetsManager.Instance é nulo!");
            }

            _gizmoDrawer = Object.FindFirstObjectByType<OrbitGizmoDrawer>();
            if (_gizmoDrawer == null)
            {
                _gizmoDrawer = new GameObject("OrbitGizmoDrawer").AddComponent<OrbitGizmoDrawer>();
                DebugUtility.Log<OrbitPlanetStrategy>("OrbitGizmoDrawer criado automaticamente.", "yellow");
            }
        }

        public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
        {
            if (!ValidateInputs(pool)) return;
            SetupSolarSystem(pool);
        }

        // Configuração do Sistema Solar
        private void SetupSolarSystem(ObjectPool pool)
        {
            int planetCount = Mathf.Min(pool.GetAvailableCount(), _maxPlanets);
            if (planetCount == 0)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>("Nenhum objeto disponível no pool!");
                return;
            }

            _orbitalRadii.Clear();
            var planetInfos = CreateAndActivatePlanets(pool, planetCount);

            if (planetInfos.Count == 0)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>("Nenhum planeta criado com sucesso!");
                return;
            }

            var orbitInfos = CalculateOrbits(planetInfos);
            PositionPlanets(planetInfos, orbitInfos);

            _gizmoDrawer.UpdateOrbitRadii(_orbitalRadii);
            DebugUtility.Log<OrbitPlanetStrategy>($"Atualizado {orbitInfos.Count} órbitas com {_orbitalRadii.Count} raios", "yellow");
        }

        // Criação e Ativação de Planetas
        private List<(PlanetsMaster planetMaster, IPoolable poolable)> CreateAndActivatePlanets(ObjectPool pool, int planetCount)
        {
            var planetInfos = new List<(PlanetsMaster planetMaster, IPoolable poolable)>();
            var resourceList = _planetsManager.GenerateResourceList(planetCount);

            for (int index = 0; index < planetCount; index++)
            {
                if (!TryGetPoolable(pool, index, out var poolable) ||
                    !TryGetPlanetData(index, resourceList, out var planetData) ||
                    !TryConfigurePlanet(poolable, planetData, index, resourceList, out var planetMaster))
                {
                    poolable?.Deactivate();
                    continue;
                }

                poolable.Activate(Vector3.zero);
                Physics.SyncTransforms();

                var planetGo = planetMaster.gameObject;
                var planetInfo = planetMaster.GetPlanetInfo();
                DebugUtility.Log<OrbitPlanetStrategy>(
                    $"Planeta {index} criado: diâmetro {planetInfo.planetDiameter:F2}, escala {planetInfo.planetScale:F2}",
                    "blue", planetGo);

                planetInfos.Add((planetMaster, poolable));
            }

            return planetInfos;
        }

        private bool TryGetPoolable(ObjectPool pool, int index, out IPoolable poolable)
        {
            poolable = pool.GetObject(Vector3.zero);
            if (poolable == null)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>($"Falha ao obter objeto do pool para planeta {index}!");
                return false;
            }
            return true;
        }

        private bool TryGetPlanetData(int index, List<PlanetResourcesSo> resourceList, out PlanetData planetData)
        {
            planetData = _planetsManager.GetRandomPlanetData();
            if (planetData == null || index >= resourceList.Count)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>(
                    planetData == null ? "Nenhum PlanetData válido encontrado!" : $"Recurso insuficiente para planeta {index}!");
                return false;
            }
            return true;
        }

        private bool TryConfigurePlanet(IPoolable poolable, PlanetData planetData, int index, List<PlanetResourcesSo> resourceList, out PlanetsMaster planetMaster)
        {
            planetMaster = _planetsManager.ConfigurePlanet(poolable, planetData, index, resourceList[index]);
            if (planetMaster == null)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>($"Falha ao configurar planeta {index}!");
                return false;
            }
            return true;
        }

        // Cálculo das Órbitas
        private List<OrbitPlanetInfo> CalculateOrbits(List<(PlanetsMaster planetMaster, IPoolable poolable)> planetInfos)
        {
            var orbitInfos = new List<OrbitPlanetInfo>();
            var usedAngles = new List<float>();

            for (int index = 0; index < planetInfos.Count; index++)
            {
                var (planetMaster, _) = planetInfos[index];
                var planetInfo = planetMaster.GetPlanetInfo();
                float planetRadius = planetInfo.planetDiameter * 0.5f;
                float prevRadius = index > 0 ? planetInfos[index - 1].planetMaster.GetPlanetInfo().planetDiameter * 0.5f : 0f;
                float prevOrbitRadius = index > 0 ? orbitInfos[index - 1].planetRadius : 0f;

                float currentRadius = index == 0
                    ? _initialOffset + planetRadius
                    : prevOrbitRadius + prevRadius + _spaceBetweenPlanets + planetRadius;

                float initialAngle = GetPlanetAngle(index, planetInfos.Count, usedAngles);
                var orbitPosition = _orbitCenter + new Vector3(Mathf.Cos(initialAngle), 0, Mathf.Sin(initialAngle)) * currentRadius;

                var orbitInfo = new OrbitPlanetInfo(
                    orbitPosition,
                    currentRadius,
                    initialAngle,
                    _orbitSpeed,
                    new Bounds(orbitPosition, Vector3.one * planetInfo.planetDiameter)
                );

                orbitInfos.Add(orbitInfo);
                _orbitalRadii.Add(currentRadius);

                if (index > 0)
                {
                    DebugUtility.Log<OrbitPlanetStrategy>($"Espaçamento órbitas {index-1}-{index}: {currentRadius - prevOrbitRadius:F2}", "cyan");
                }

                DebugUtility.Log<OrbitPlanetStrategy>(
                    $"Planeta {index} - Raio orbital: {currentRadius:F2}, Ângulo: {initialAngle * Mathf.Rad2Deg:F1}°, Raio planeta: {planetRadius:F2}, Escala: {planetInfo.planetScale:F2}",
                    "green");
                DebugUtility.Log<OrbitPlanetStrategy>(
                    $"Cálculo orbital: prevOrbitRadius={prevOrbitRadius:F2}, prevRadius={prevRadius:F2}, space={_spaceBetweenPlanets:F2}, planetRadius={planetRadius:F2}, total={currentRadius:F2}",
                    "yellow");
            }

            return orbitInfos;
        }

        // Posicionamento dos Planetas
        private void PositionPlanets(List<(PlanetsMaster planetMaster, IPoolable poolable)> planetInfos, List<OrbitPlanetInfo> orbitInfos)
        {
            for (int index = 0; index < planetInfos.Count; index++)
            {
                var (planetMaster, poolable) = planetInfos[index];
                var orbitInfo = orbitInfos[index];
                var planetInfo = planetMaster.GetPlanetInfo();

                // Verifica colisões apenas como segurança
                var (adjustedPosition, adjustedRadius) = ValidatePosition(index, planetInfos, orbitInfos, orbitInfo);

                planetInfo.orbitPosition = adjustedPosition;
                planetInfo.planetRadius = adjustedRadius;
                planetInfo.initialAngle = orbitInfo.initialAngle;
                planetInfo.orbitSpeed = orbitInfo.orbitSpeed;

                poolable.Activate(adjustedPosition);
                EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(planetMaster));

                DebugUtility.Log<OrbitPlanetStrategy>(
                    $"Planeta {index} posicionado em {adjustedPosition} com raio orbital {adjustedRadius:F2}",
                    "green", planetMaster.gameObject);
            }
        }

        private (Vector3 position, float radius) ValidatePosition(int index, List<(PlanetsMaster planetMaster, IPoolable poolable)> planetInfos,
            List<OrbitPlanetInfo> orbitInfos, OrbitPlanetInfo orbitInfo)
        {
            Vector3 adjustedPosition = orbitInfo.orbitPosition;
            float adjustedRadius = orbitInfo.planetRadius;
            var planetInfo = planetInfos[index].planetMaster.GetPlanetInfo();
            int attempts = 0;

            while (attempts < MaxCollisionAttempts)
            {
                bool hasCollision = false;
                for (int prevIndex = 0; prevIndex < index; prevIndex++)
                {
                    var prevOrbitInfo = orbitInfos[prevIndex];
                    var prevPlanetInfo = planetInfos[prevIndex].planetMaster.GetPlanetInfo();
                    float distance = Vector3.Distance(adjustedPosition, prevOrbitInfo.orbitPosition);
                    float minDistance = (planetInfo.planetDiameter * 0.5f) + (prevPlanetInfo.planetDiameter * 0.5f) + _spaceBetweenPlanets;

                    if (distance < minDistance)
                    {
                        hasCollision = true;
                        adjustedRadius += _spaceBetweenPlanets * 0.5f; // Ajuste menor para minimizar deslocamentos
                        adjustedPosition = _orbitCenter + new Vector3(Mathf.Cos(orbitInfo.initialAngle), 0, Mathf.Sin(orbitInfo.initialAngle)) * adjustedRadius;
                        attempts++;
                        DebugUtility.Log<OrbitPlanetStrategy>(
                            $"Colisão detectada para planeta {index} com planeta {prevIndex}. Novo raio: {adjustedRadius:F2}",
                            "yellow");
                        break;
                    }
                }

                if (!hasCollision) break;
            }

            if (attempts >= MaxCollisionAttempts)
            {
                DebugUtility.LogWarning<OrbitPlanetStrategy>($"Não foi possível evitar colisão para planeta {index} após {attempts} tentativas!");
            }

            return (adjustedPosition, adjustedRadius);
        }

        // Validação de Entrada
        private bool ValidateInputs(ObjectPool pool)
        {
            if (pool == null || _planetsManager == null)
            {
                DebugUtility.LogError<OrbitPlanetStrategy>(pool == null ? "Pool é nulo!" : "PlanetsManager.Instance é nulo!");
                return false;
            }
            return true;
        }

        // Cálculo de Ângulos
        private float GetPlanetAngle(int planetIndex, int totalPlanets, List<float> usedAngles)
        {
            return _useRandomAngles ? GetRandomAngleWithValidation(usedAngles, totalPlanets) : GetOptimalAngle(planetIndex, totalPlanets);
        }

        private float GetOptimalAngle(int planetIndex, int totalPlanets)
        {
            float baseAngle = (FullCircleDegrees / totalPlanets) * planetIndex;
            if (_addAngleVariation)
            {
                baseAngle += Random.Range(-_angleVariationDegrees, _angleVariationDegrees);
            }
            baseAngle = NormalizeAngle(baseAngle);
            float finalAngle = baseAngle * Mathf.Deg2Rad;
            DebugUtility.Log<OrbitPlanetStrategy>($"Ângulo ótimo: {baseAngle:F1}° para planeta {planetIndex}/{totalPlanets}", "blue");
            return finalAngle;
        }

        private float GetRandomAngleWithValidation(List<float> usedAngles, int totalPlanets)
        {
            if (usedAngles.Count == 0)
            {
                float firstAngle = Random.Range(0f, FullCircleDegrees) * Mathf.Deg2Rad;
                DebugUtility.Log<OrbitPlanetStrategy>($"Primeiro ângulo aleatório: {firstAngle * Mathf.Rad2Deg:F1}°", "blue");
                usedAngles.Add(firstAngle);
                return firstAngle;
            }

            float minAngleSeparation = Mathf.Max(FullCircleDegrees / (totalPlanets * AngleSeparationFactor), _minAngleSeparationDegrees);
            for (int attempts = 0; attempts < _maxAngleAttempts; attempts++)
            {
                float candidateAngle = Random.Range(0f, FullCircleDegrees) * Mathf.Deg2Rad;
                if (IsAngleValid(candidateAngle, usedAngles, minAngleSeparation))
                {
                    DebugUtility.Log<OrbitPlanetStrategy>($"Ângulo aleatório válido: {candidateAngle * Mathf.Rad2Deg:F1}° (tentativa {attempts + 1})", "blue");
                    usedAngles.Add(candidateAngle);
                    return candidateAngle;
                }
            }

            float fallbackAngle = GetEquidistantFallbackAngle(usedAngles.Count, totalPlanets);
            usedAngles.Add(fallbackAngle);
            DebugUtility.LogWarning<OrbitPlanetStrategy>($"Ângulo fallback: {fallbackAngle * Mathf.Rad2Deg:F1}° após {_maxAngleAttempts} tentativas");
            return fallbackAngle;
        }

        private bool IsAngleValid(float candidateAngle, List<float> usedAngles, float minSeparation)
        {
            float candidateAngleDegrees = candidateAngle * Mathf.Rad2Deg;
            return usedAngles.All(usedAngle => Mathf.Abs(Mathf.DeltaAngle(candidateAngleDegrees, usedAngle * Mathf.Rad2Deg)) >= minSeparation);
        }

        private float GetEquidistantFallbackAngle(int planetIndex, int totalPlanets)
        {
            return (FullCircleDegrees / totalPlanets) * planetIndex * Mathf.Deg2Rad;
        }

        private float NormalizeAngle(float angle)
        {
            angle %= FullCircleDegrees;
            return angle < 0f ? angle + FullCircleDegrees : angle;
        }
    }
}