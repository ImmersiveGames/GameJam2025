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
        public void ResolvedWaveProfile_ShouldCloneWithSpawnPatternAndCache()
        {
            var preset = ScriptableObject.CreateInstance<PlanetDefensePresetSo>();
            var baseProfile = ScriptableObject.CreateInstance<DefenseWaveProfileSo>();
            baseProfile.enemiesPerWave = 8;
            baseProfile.secondsBetweenWaves = 7;
            baseProfile.spawnRadius = 6f;
            baseProfile.spawnHeightOffset = 1.5f;

            var spawnPattern = ScriptableObject.CreateInstance<MockDefenseSpawnPattern>();

            SetPrivateField(preset, "baseWaveProfile", baseProfile);
            SetPrivateField(preset, "spawnPatternOverride", spawnPattern);

            var resolved1 = preset.ResolvedWaveProfile;
            var resolved2 = preset.ResolvedWaveProfile;

            Assert.AreSame(resolved1, resolved2, "Resolved wave profile should be cached for repeated calls.");
            Assert.AreNotSame(baseProfile, resolved1, "Spawn pattern override should trigger runtime clone.");
            Assert.AreEqual(baseProfile.enemiesPerWave, resolved1.enemiesPerWave);
            Assert.AreEqual(baseProfile.secondsBetweenWaves, resolved1.secondsBetweenWaves);
            Assert.AreEqual(baseProfile.spawnRadius, resolved1.spawnRadius);
            Assert.AreEqual(baseProfile.spawnHeightOffset, resolved1.spawnHeightOffset);
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
