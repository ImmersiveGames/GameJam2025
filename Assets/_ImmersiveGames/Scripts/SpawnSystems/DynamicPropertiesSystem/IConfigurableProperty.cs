using System;
namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    public interface IConfigurableProperty
    {
        string Name { get; }
        Type PropertyType { get; }
        object GetValue();
        void SetValue(object value);
        bool IsRequired { get; }
        string Description { get; }
    }
}