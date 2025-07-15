using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Data
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/Enhanced/StrategyData")]
    public class EnhancedStrategyData : ScriptableObject
    {
        [Header("Strategy Configuration")]
        public StrategyType strategyType;
        
        [Header("Dynamic Properties")]
        [SerializeField] private DynamicProperties dynamicProperties = new();

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
        
        public void RemoveProperty(string nameProperties)
        {
            dynamicProperties.RemoveProperty(nameProperties);
        }
    }
}