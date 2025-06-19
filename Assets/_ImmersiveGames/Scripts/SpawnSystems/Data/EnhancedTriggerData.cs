using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/Enhanced/TriggerData")]
    public class EnhancedTriggerData : ScriptableObject
    {
        [Header("Trigger Configuration")]
        public TriggerType triggerType;

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
            var template = PropertyTemplateRegistry.GetTriggerTemplate(triggerType);
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
        public bool HasProperty(string nameProperties)
        {
            return dynamicProperties.HasProperty(nameProperties);
        }
    }
}