using _ImmersiveGames.Scripts.ResourceSystems.Services;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Definition")]
    public class ResourceDefinition : ScriptableObject
    {
        [Header("Basic Settings")]
        public ResourceType type;
        public int initialValue = 100;
        public int maxValue = 100;
        public bool enabled = true;

        [Header("UI Settings")]
        public Sprite icon; // Ícone padrão, pode ser sobrescrito por instância se necessário

        public ResourceType ResourceCategory => type;
        public Sprite Icon => icon;

        public virtual IResourceValue CreateInitialValue()
        {
            return new BasicResourceValue(initialValue, maxValue);
        }

        public virtual void ApplyTo(IResourceValue value)
        {
            // Implementações específicas podem ajustar dados adicionais (ex.: ícones).
        }
    }

    public enum ResourceType
    {
        Health,
        Energy,
        Mana,
        Stamina,
        Hungry,
        PlanetResource,
        None
    }
}
