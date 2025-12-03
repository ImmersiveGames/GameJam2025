using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Tests
{
    /// <summary>
    /// Valida SRP: preset não injeta comportamento de minion e estratégia não muta o contexto.
    /// </summary>
    public class TestSRPCompliance
    {
        [Test]
        public void Strategy_ShouldNotMutateContext()
        {
            var planet = new GameObject("Planet").AddComponent<PlanetsMaster>();
            var detection = ScriptableObject.CreateInstance<DetectionType>();
            var strategy = new SimplePlanetDefenseStrategy(DefenseTargetMode.PreferPlayer);
            var context = new PlanetDefenseSetupContext(planet, detection, null, strategy, null, null, null);

            strategy.ConfigureContext(context);

            Assert.AreSame(strategy, context.Strategy, "Strategy must not replace context strategy.");
            Object.DestroyImmediate(planet.gameObject);
        }

        [Test]
        public void Preset_ShouldExposeMinionDataWithoutBehaviorMutation()
        {
            var preset = ScriptableObject.CreateInstance<PlanetDefensePresetSo>();
            var minionData = ScriptableObject.CreateInstance<DefensesMinionData>();

            SetPrivateField(preset, "minionData", minionData);

            Assert.AreSame(minionData, preset.MinionData, "Preset must return the assigned minion data (SRP).");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
