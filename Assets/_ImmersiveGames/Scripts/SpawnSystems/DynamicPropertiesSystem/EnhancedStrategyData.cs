using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/Enhanced/StrategyData")]
    public class EnhancedStrategyData : ScriptableObject
    {
        [Header("Strategy Configuration")]
        public StrategyType strategyType;
        
        [Header("Dynamic Properties")]
        [SerializeField] private DynamicProperties dynamicProperties = new DynamicProperties();

        public T GetProperty<T>(string nameProperties, T defaultValue = default)
        {
            return dynamicProperties.GetProperty(nameProperties, defaultValue);
        }

        public void SetProperty<T>(string nameProperties, T value, bool isRequired = false, string description = "")
        {
            dynamicProperties.SetProperty(nameProperties, value, isRequired, description);
        }

        public void ApplyTemplate()
        {
            var template = PropertyTemplateRegistry.GetStrategyTemplate(strategyType);
            if (template != null)
            {
                dynamicProperties.ApplyTemplate(template);
            }
        }

        public IEnumerable<IConfigurableProperty> GetAllProperties()
        {
            return dynamicProperties.GetAllProperties();
        }

        // Compatibilidade com o sistema antigo
        public StrategyProperties GetLegacyProperties()
        {
            var props = new StrategyProperties
            {
                radius = GetProperty("radius", 2f),
                space = GetProperty("space", 5f),
                spawnArea = GetProperty("spawnArea", new Vector2(5f, 5f)),
                waveInterval = GetProperty("waveInterval", 1f),
                waveCount = GetProperty("waveCount", 3),
                useRandomAngles = GetProperty("useRandomAngles", false),
                addAngleVariation = GetProperty("addAngleVariation", true),
                interval = GetProperty("interval", 1f)
            };
            return props;
        }
        public void RemoveProperty(string nameProperties)
        {
            dynamicProperties.RemoveProperty(nameProperties);
        }
    }
}