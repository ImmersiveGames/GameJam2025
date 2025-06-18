// ===== SISTEMA DE PROPRIEDADES DINÂMICAS =====

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    [Serializable]
    public class DynamicProperties
    {
        [SerializeReference] private List<IConfigurableProperty> properties = new List<IConfigurableProperty>();

        public T GetProperty<T>(string name, T defaultValue = default)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            if (prop != null && prop.GetValue() is T value)
                return value;
            return defaultValue;
        }

        public void SetProperty<T>(string name, T value, bool isRequired = false, string description = "")
        {
            var existingProp = properties.FirstOrDefault(p => p.Name == name);
            if (existingProp != null)
            {
                existingProp.SetValue(value);
                return;
            }

            // Criar nova propriedade baseada no tipo
            IConfigurableProperty newProp = value switch
            {
                float f => new FloatProperty(name, f, isRequired, description),
                int i => new IntProperty(name, i, isRequired, description),
                bool b => new BoolProperty(name, b, isRequired, description),
                string s => new StringProperty(name, s, isRequired, description),
                Vector2 v2 => new Vector2Property(name, v2, isRequired, description),
                Vector3 v3 => new Vector3Property(name, v3, isRequired, description),
                _ => null
            };

            if (newProp != null)
                properties.Add(newProp);
        }

        public IEnumerable<IConfigurableProperty> GetAllProperties() => properties;

        public void ClearProperties() => properties.Clear();

        public bool HasProperty(string name) => properties.Any(p => p.Name == name);

        public void RemoveProperty(string name)
        {
            var prop = properties.FirstOrDefault(p => p.Name == name);
            if (prop != null)
                properties.Remove(prop);
        }

        public void ApplyTemplate(PropertyTemplate template)
        {
            ClearProperties();
            foreach (var prop in template.Properties)
            {
                properties.Add(prop);
            }
        }
    }

    // ===== TEMPLATES DE PROPRIEDADES =====
    [Serializable]
    public class PropertyTemplate
    {
        [SerializeField] private string templateName;
        [SerializeReference] private List<IConfigurableProperty> properties = new List<IConfigurableProperty>();

        public string TemplateName => templateName;
        public IEnumerable<IConfigurableProperty> Properties => properties;

        public PropertyTemplate(string name)
        {
            templateName = name;
        }

        public PropertyTemplate AddProperty<T>(string name, T defaultValue, bool isRequired = false, string description = "")
        {
            IConfigurableProperty prop = defaultValue switch
            {
                float f => new FloatProperty(name, f, isRequired, description),
                int i => new IntProperty(name, i, isRequired, description),
                bool b => new BoolProperty(name, b, isRequired, description),
                string s => new StringProperty(name, s, isRequired, description),
                Vector2 v2 => new Vector2Property(name, v2, isRequired, description),
                Vector3 v3 => new Vector3Property(name, v3, isRequired, description),
                _ => null
            };

            if (prop != null)
                properties.Add(prop);

            return this;
        }
    }

    // ===== REGISTRY DE TEMPLATES =====
    public static class PropertyTemplateRegistry
    {
        private static readonly Dictionary<TriggerType, PropertyTemplate> triggerTemplates = new();
        private static readonly Dictionary<StrategyType, PropertyTemplate> strategyTemplates = new();

        static PropertyTemplateRegistry()
        {
            InitializeTriggerTemplates();
            InitializeStrategyTemplates();
        }

        private static void InitializeTriggerTemplates()
        {
            triggerTemplates[TriggerType.InitializationTrigger] = new PropertyTemplate("InitializationTrigger")
                .AddProperty("delay", 0f, false, "Atraso inicial em segundos antes do spawn");

            triggerTemplates[TriggerType.IntervalTrigger] = new PropertyTemplate("IntervalTrigger")
                .AddProperty("interval", 2f, true, "Intervalo entre spawns em segundos")
                .AddProperty("startImmediately", true, false, "Iniciar imediatamente ao ativar");

            triggerTemplates[TriggerType.InputSystemTrigger] = new PropertyTemplate("InputSystemTrigger")
                .AddProperty("actionName", "Fire", true, "Nome da ação no Input System");

            triggerTemplates[TriggerType.GlobalEventTrigger] = new PropertyTemplate("GlobalEventTrigger")
                .AddProperty("eventName", "GlobalSpawnEvent", true, "Nome do evento global a escutar");

            triggerTemplates[TriggerType.PredicateTrigger] = new PropertyTemplate("PredicateTrigger")
                .AddProperty("checkInterval", 0.5f, true, "Intervalo de verificação do predicado em segundos");
        }

        private static void InitializeStrategyTemplates()
        {
            strategyTemplates[StrategyType.SimpleSpawnStrategy] = new PropertyTemplate("SimpleSpawnStrategy")
                .AddProperty("offset", Vector3.zero, false, "Deslocamento relativo à posição do SpawnPoint")
                .AddProperty("spawnCount", 1, true, "Número de objetos a spawnar");

            strategyTemplates[StrategyType.DirectionalSpawnStrategy] = new PropertyTemplate("DirectionalSpawnStrategy")
                .AddProperty("speed", 5f, true, "Velocidade inicial do objeto")
                .AddProperty("offset", Vector3.zero, false, "Deslocamento relativo à posição do SpawnPoint")
                .AddProperty("spawnCount", 1, true, "Número de objetos a spawnar");

            strategyTemplates[StrategyType.FullPoolSpawnStrategy] = new PropertyTemplate("FullPoolSpawnStrategy")
                .AddProperty("spacing", 1f, true, "Espaçamento entre objetos spawnados");

            // Exemplo de nova estratégia - super fácil!
            // strategyTemplates[StrategyType.SpiralStrategy] = new PropertyTemplate("SpiralStrategy")
            //     .AddProperty("spiralRadius", 5f, true, "Raio da espiral")
            //     .AddProperty("spiralSpeed", 2f, false, "Velocidade da rotação")
            //     .AddProperty("spiralSteps", 8, false, "Passos na espiral");
        }

        public static PropertyTemplate GetTriggerTemplate(TriggerType type)
        {
            return triggerTemplates.GetValueOrDefault(type);
        }

        public static PropertyTemplate GetStrategyTemplate(StrategyType type)
        {
            return strategyTemplates.GetValueOrDefault(type);
        }

        public static void RegisterTriggerTemplate(TriggerType type, PropertyTemplate template)
        {
            triggerTemplates[type] = template;
        }

        public static void RegisterStrategyTemplate(StrategyType type, PropertyTemplate template)
        {
            strategyTemplates[type] = template;
        }
    }
    
}