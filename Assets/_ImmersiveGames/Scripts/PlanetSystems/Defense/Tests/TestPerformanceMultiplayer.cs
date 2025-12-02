using NUnit.Framework;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Tests
{
    /// <summary>
    /// Valida otimizações de cache para multiplayer local, reduzindo alocações repetidas.
    /// </summary>
    public class TestPerformanceMultiplayer
    {
        [SetUp]
        public void SetUp()
        {
            PlanetDefensePresetAdapter.ClearAll();
        }

        [Test]
        public void Resolve_ShouldReuseContextForSamePlanet()
        {
            var planet = new GameObject("PlanetCache").AddComponent<PlanetsMaster>();
            var detection = ScriptableObject.CreateInstance<DetectionType>();
            var preset = ScriptableObject.CreateInstance<PlanetDefensePresetSo>();

            var first = PlanetDefensePresetAdapter.Resolve(planet, detection, preset);
            var second = PlanetDefensePresetAdapter.Resolve(planet, detection, preset);

            Assert.AreSame(first, second, "Context should be reused for identical parameters in local multiplayer.");

            PlanetDefensePresetAdapter.ClearCache(planet);
            var third = PlanetDefensePresetAdapter.Resolve(planet, detection, preset);

            Assert.AreNotSame(first, third, "Clearing cache should force context recreation.");

            Object.DestroyImmediate(planet.gameObject);
        }
    }
}
