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
        private static readonly Dictionary<TriggerType, PropertyTemplate> _triggerTemplates = new();
        private static readonly Dictionary<StrategyType, PropertyTemplate> _strategyTemplates = new();

        static PropertyTemplateRegistry()
        {
            InitializeTriggerTemplates();
            InitializeStrategyTemplates();
        }

        private static void InitializeTriggerTemplates()
        {
            _triggerTemplates[TriggerType.InitializationTrigger] = new PropertyTemplate("InitializationTrigger")
                .AddProperty("delay", 0f, false, "Atraso inicial em segundos antes do spawn");

            _triggerTemplates[TriggerType.IntervalTrigger] = new PropertyTemplate("IntervalTrigger")
                .AddProperty("interval", 2f, true, "Intervalo entre spawns em segundos")
                .AddProperty("startImmediately", true, false, "Iniciar imediatamente ao ativar");

            _triggerTemplates[TriggerType.InputSystemTrigger] = new PropertyTemplate("InputSystemTrigger")
                .AddProperty("actionName", "Fire", true, "Nome da ação no Input System");

            _triggerTemplates[TriggerType.GlobalEventTrigger] = new PropertyTemplate("GlobalEventTrigger")
                .AddProperty("eventName", "GlobalSpawnEvent", true, "Nome do evento global a escutar");
            _triggerTemplates[TriggerType.GenericGlobalEventTrigger] = new PropertyTemplate("GenericGlobalEventTrigger")
                .AddProperty("eventName", "GlobalGenericSpawnEvent", true, "Nome do evento global generic a escutar")
                .AddProperty("useGenericTrigger", false, false, "Usar GenericGlobalEventTrigger para eventos sem Position/GameObject");
            _triggerTemplates[TriggerType.PredicateTrigger] = new PropertyTemplate("PredicateTrigger")
                .AddProperty("checkInterval", 0.5f, true, "Intervalo de verificação do predicado em segundos");
        }

        private static void InitializeStrategyTemplates()
        {
            _strategyTemplates[StrategyType.SimpleSpawnStrategy] = new PropertyTemplate("SimpleSpawnStrategy")
                .AddProperty("offset", Vector3.zero, false, "Deslocamento relativo à posição do SpawnPoint")
                .AddProperty("spawnCount", 1, true, "Número de objetos a spawnar");

            _strategyTemplates[StrategyType.DirectionalSpawnStrategy] = new PropertyTemplate("DirectionalSpawnStrategy")
                .AddProperty("offset", Vector3.zero, false, "Deslocamento relativo à posição do SpawnPoint")
                .AddProperty("spawnCount", 1, true, "Número de objetos a spawnar")
                .AddProperty("randomizeDirection", false, false, "Randomizar direção dos objetos")
                .AddProperty("directionVariation", 0f, false, "Variação máxima da direção (graus)");
            
            _strategyTemplates[StrategyType.FullPoolSpawnStrategy] = new PropertyTemplate("FullPoolSpawnStrategy")
                .AddProperty("spacing", 1f, true, "Espaçamento entre objetos spawnados");
            _strategyTemplates[StrategyType.OrbitPlanetStrategy] = new PropertyTemplate("OrbitPlanetStrategy")
                .AddProperty("minAngleSeparationDegrees", 10f, true, "Separação mínima entre ângulos dos planetas (graus)")
                .AddProperty("angleVariationDegrees", 10f, true, "Variação máxima de ângulo para ângulos otimizados (graus)")
                .AddProperty("maxAngleAttempts", 50, true, "Máximo de tentativas para encontrar ângulo aleatório válido")
                .AddProperty("useRandomAngles", false, false, "Usar ângulos aleatórios em vez de otimizados")
                .AddProperty("addAngleVariation", false, false, "Adicionar variação aos ângulos otimizados")
                .AddProperty("initialOffset", 10f, true, "Offset inicial do centro da órbita")
                .AddProperty("orbitCenter", Vector3.zero, false, "Centro da órbita")
                .AddProperty("spaceBetweenPlanets", 2f, true, "Espaço mínimo entre planetas")
                .AddProperty("maxPlanets", 10, true, "Número máximo de planetas")
                .AddProperty("orbitSpeed", 10f, true, "Velocidade de rotação dos planetas (graus/segundo)");
        }

        public static PropertyTemplate GetTriggerTemplate(TriggerType type)
        {
            return _triggerTemplates.GetValueOrDefault(type);
        }

        public static PropertyTemplate GetStrategyTemplate(StrategyType type)
        {
            return _strategyTemplates.GetValueOrDefault(type);
        }

        public static void RegisterTriggerTemplate(TriggerType type, PropertyTemplate template)
        {
            _triggerTemplates[type] = template;
        }

        public static void RegisterStrategyTemplate(StrategyType type, PropertyTemplate template)
        {
            _strategyTemplates[type] = template;
        }
    }
    
}