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
    }
}