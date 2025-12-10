using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Responsável por calcular e aplicar a disposição orbital dos planetas
    /// em torno de um centro, bem como configurar o módulo PlanetMotion
    /// de cada planeta quando disponível.
    ///
    /// Esta classe não instancia planetas e não cuida de recursos;
    /// apenas organiza posição e movimento orbital/rotação própria.
    /// </summary>
    public sealed class PlanetOrbitArranger
    {
        private readonly Transform _centerRoot;
        private readonly float _initialOrbitRadius;
        private readonly float _minimumOrbitSpacing;
        private readonly bool _randomizeInitialAngle;
        private readonly Vector2 _orbitSpeedRange;
        private readonly Vector2 _selfRotationSpeedRange;
        private readonly bool _randomizeOrbitDirection;
        private readonly bool _defaultOrbitClockwise;

        public PlanetOrbitArranger(
            Transform centerRoot,
            float initialOrbitRadius,
            float minimumOrbitSpacing,
            bool randomizeInitialAngle,
            Vector2 orbitSpeedRange,
            Vector2 selfRotationSpeedRange,
            bool randomizeOrbitDirection,
            bool defaultOrbitClockwise)
        {
            _centerRoot = centerRoot;
            _initialOrbitRadius = Mathf.Max(0f, initialOrbitRadius);
            _minimumOrbitSpacing = Mathf.Max(0f, minimumOrbitSpacing);
            _randomizeInitialAngle = randomizeInitialAngle;
            _orbitSpeedRange = orbitSpeedRange;
            _selfRotationSpeedRange = selfRotationSpeedRange;
            _randomizeOrbitDirection = randomizeOrbitDirection;
            _defaultOrbitClockwise = defaultOrbitClockwise;
        }

        /// <summary>
        /// Calcula e aplica as órbitas dos planetas recebidos, retornando
        /// a lista de raios de órbita utilizados (um por planeta).
        /// </summary>
        /// <param name="planets">Lista de PlanetsMaster a serem organizados.</param>
        /// <returns>Lista de raios de órbita (mesma ordem da lista de entrada).</returns>
        public List<float> ArrangePlanetsInOrbits(IList<PlanetsMaster> planets)
        {
            var orbitRadii = new List<float>();

            if (planets == null || planets.Count == 0)
            {
                return orbitRadii;
            }

            var centerPosition = _centerRoot != null ? _centerRoot.position : Vector3.zero;

            float previousOrbitRadius = 0f;
            float previousPlanetRadius = 0f;

            int planetCount = planets.Count;

            for (int i = 0; i < planetCount; i++)
            {
                var planet = planets[i];
                if (planet == null)
                {
                    orbitRadii.Add(0f);
                    continue;
                }

                float planetRadius = CalculatePlanetRadius(planet.gameObject);

                float orbitRadius = i == 0
                    ? Mathf.Max(_initialOrbitRadius, planetRadius)
                    : Mathf.Max(
                        _initialOrbitRadius,
                        previousOrbitRadius + _minimumOrbitSpacing + previousPlanetRadius + planetRadius);

                float angle = _randomizeInitialAngle
                    ? Random.Range(0f, Mathf.PI * 2f)
                    : Mathf.PI * 2f * (i / (float)planetCount);

                Vector3 offset = new(
                    Mathf.Cos(angle) * orbitRadius,
                    0f,
                    Mathf.Sin(angle) * orbitRadius);

                planet.transform.position = centerPosition + offset;

                ConfigurePlanetMotion(planet, orbitRadius, angle);

                orbitRadii.Add(orbitRadius);

                previousOrbitRadius = orbitRadius;
                previousPlanetRadius = planetRadius;
            }

            return orbitRadii;
        }

        /// <summary>
        /// Configura o componente PlanetMotion (se existir) com as
        /// informações de órbita calculadas.
        /// </summary>
        private void ConfigurePlanetMotion(PlanetsMaster planet, float orbitRadius, float startAngle)
        {
            if (planet == null)
            {
                return;
            }

            if (!planet.TryGetComponent(out PlanetMotion motion))
            {
                return;
            }

            float orbitSpeed = GetRandomSpeedFromRange(_orbitSpeedRange);
            float selfRotationSpeed = GetRandomSpeedFromRange(_selfRotationSpeedRange);
            bool orbitClockwise = _randomizeOrbitDirection ? Random.value > 0.5f : _defaultOrbitClockwise;

            motion.ConfigureOrbit(
                _centerRoot,
                orbitRadius,
                startAngle,
                orbitSpeed,
                selfRotationSpeed,
                orbitClockwise);
        }

        /// <summary>
        /// Calcula um raio aproximado do planeta, usando bounds reais
        /// quando possível e fallback para escala se necessário.
        /// </summary>
        private static float CalculatePlanetRadius(GameObject planetObject)
        {
            if (planetObject == null)
            {
                return 0f;
            }

            var bounds = CalculateRealLength.GetBounds(planetObject);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            if (radius > 0f)
            {
                return radius;
            }

            var scale = planetObject.transform.lossyScale;
            radius = Mathf.Max(scale.x, scale.z) * 0.5f;

            return radius > 0f ? radius : 0.5f;
        }

        private static float GetRandomSpeedFromRange(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);

            if (Mathf.Approximately(min, max))
            {
                return min;
            }

            return Random.Range(min, max);
        }
    }
}
