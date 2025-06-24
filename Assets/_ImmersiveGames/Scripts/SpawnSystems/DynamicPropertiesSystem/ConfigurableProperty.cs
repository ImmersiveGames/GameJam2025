using System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    [Serializable]
    public class ConfigurableProperty<T> : IConfigurableProperty
    {
        [SerializeField] private string name;
        [SerializeField] private T value;
        [SerializeField] private bool isRequired;
        [SerializeField] private string description;

        public string Name => name;
        public Type PropertyType => typeof(T);
        public bool IsRequired => isRequired;
        public string Description => description;

        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public object GetValue() => value;
        public void SetValue(object val)
        {
            try
            {
                if (typeof(T).IsEnum && val is string stringValue)
                {
                    if (Enum.TryParse(typeof(T), stringValue, true, out var enumValue))
                        value = (T)enumValue;
                    else
                        DebugUtility.LogError<ConfigurableProperty<T>>($"Falha ao converter '{stringValue}' para {typeof(T).Name}.");
                }
                else
                {
                    value = (T)Convert.ChangeType(val, typeof(T));
                }
            }
            catch (Exception e)
            {
                DebugUtility.LogError<ConfigurableProperty<T>>($"Erro ao definir valor para propriedade '{name}': {e.Message}");
            }
        }

        public ConfigurableProperty(string name, T defaultValue, bool isRequired = false, string description = "")
        {
            this.name = name;
            this.value = defaultValue;
            this.isRequired = isRequired;
            this.description = description;
        }
    }

    [Serializable]
    public class FloatProperty : ConfigurableProperty<float>
    {
        public FloatProperty(string name, float defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class IntProperty : ConfigurableProperty<int>
    {
        public IntProperty(string name, int defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class BoolProperty : ConfigurableProperty<bool>
    {
        public BoolProperty(string name, bool defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class StringProperty : ConfigurableProperty<string>
    {
        public StringProperty(string name, string defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class Vector2Property : ConfigurableProperty<Vector2>
    {
        public Vector2Property(string name, Vector2 defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class Vector3Property : ConfigurableProperty<Vector3>
    {
        public Vector3Property(string name, Vector3 defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }

    [Serializable]
    public class EnumProperty<TEnum> : ConfigurableProperty<TEnum> where TEnum : Enum
    {
        public EnumProperty(string name, TEnum defaultValue, bool isRequired = false, string description = "")
            : base(name, defaultValue, isRequired, description) { }
    }
}