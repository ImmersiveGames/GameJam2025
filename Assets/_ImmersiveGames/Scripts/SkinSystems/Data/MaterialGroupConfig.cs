using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    [CreateAssetMenu(fileName = "MaterialGroupConfig", menuName = "ImmersiveGames/Skin/MaterialGroupConfig", order = 4)]
    public class MaterialGroupConfig : ScriptableObject
    {
        [Header("Group Settings")]
        [SerializeField] private string groupName = "Material Group";
        [SerializeField] private Material[] availableMaterials;

        [Header("Application Settings")]
        [SerializeField] private bool randomizeOnApply = true;

        public string GroupName => groupName;
        public Material[] AvailableMaterials => availableMaterials;
        public bool RandomizeOnApply => randomizeOnApply;

        /// <summary>
        /// Obtém um material aleatório do grupo
        /// </summary>
        public Material GetRandomMaterial()
        {
            if (availableMaterials == null || availableMaterials.Length == 0)
                return null;
            
            return availableMaterials[Random.Range(0, availableMaterials.Length)];
        }

        /// <summary>
        /// Obtém um material específico por índice (útil para progressão)
        /// </summary>
        public Material GetMaterialByIndex(int index)
        {
            if (availableMaterials == null || availableMaterials.Length == 0)
                return null;
            
            return availableMaterials[Mathf.Clamp(index, 0, availableMaterials.Length - 1)];
        }

        /// <summary>
        /// Verifica se o grupo tem materiais disponíveis
        /// </summary>
        public bool HasMaterials => availableMaterials is { Length: > 0 };
    }
}