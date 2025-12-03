using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Tests
{
    /// <summary>
    /// Verifica composição de wave profile no preset e o cache de runtime para spawn pattern.
    /// </summary>
    public class TestDefenseWaves
    {
        [Test]
        public void ResolvedWaveProfile_ShouldComposeWithSpawnPatternAndCache()
        {
            var preset = ScriptableObject.CreateInstance<PlanetDefensePresetSo>();

            var spawnPattern = ScriptableObject.CreateInstance<MockDefenseSpawnPattern>();

            SetPrivateField(preset, "planetDefenseWaveEnemiesCount", 8);
            SetPrivateField(preset, "planetDefenseWaveSecondsBetweenWaves", 7);
            SetPrivateField(preset, "planetDefenseWaveSpawnRadius", 6f);
            SetPrivateField(preset, "planetDefenseWaveSpawnHeightOffset", 1.5f);
            SetPrivateField(preset, "planetDefenseWaveSpawnPattern", spawnPattern);

            var resolved1 = preset.ResolvedWaveProfile;
            var resolved2 = preset.ResolvedWaveProfile;

            Assert.AreSame(resolved1, resolved2, "Resolved wave profile should be cached for repeated calls.");
            Assert.AreEqual(8, resolved1.enemiesPerWave);
            Assert.AreEqual(7, resolved1.secondsBetweenWaves);
            Assert.AreEqual(6f, resolved1.spawnRadius);
            Assert.AreEqual(1.5f, resolved1.spawnHeightOffset);
            Assert.AreEqual(spawnPattern, resolved1.spawnPattern);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private sealed class MockDefenseSpawnPattern : DefenseSpawnPatternSo
        {
            public override Vector3 GetSpawnOffset(int index, int total, float radius, float heightOffset)
            {
                return Vector3.zero;
            }
        }
    }
}
