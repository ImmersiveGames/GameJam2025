using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/RuntimeAttribute/RuntimeAttribute Definition")]
    public class RuntimeAttributeDefinition : ScriptableObject
    {
        [Header("Basic Settings")]
        public RuntimeAttributeType type;
        public int initialValue = 100;
        public int maxValue = 100;
        public bool enabled = true;

        [Header("UI Settings")]
        public Sprite icon; // Ícone padrão, pode ser sobrescrito por instância se necessário
    }
    public enum RuntimeAttributeType
    {
        Health,
        Energy,
        Mana,
        Stamina,
        Hungry,
        None
    }
}