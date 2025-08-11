using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    [CreateAssetMenu(fileName = "SkinConfigData", menuName = "ImmersiveGames/Skin/SkinConfigData", order = 4)]
    public class SkinConfigData : ScriptableObject, ISkinConfig
    {
        [SerializeField] private string configName = "Skin";
        [SerializeField] private List<GameObject> modelPrefabs = new List<GameObject>();
        [SerializeField] private ModelType modelType = ModelType.ModelRoot;
        [SerializeField] private InstantiationMode instantiationMode = InstantiationMode.First;
        [SerializeField] private int specificIndex;

        public string ConfigName => configName;
        public ModelType ModelType => modelType;
        public List<GameObject> ModelPrefabs => modelPrefabs;
        public InstantiationMode InstantiationMode => instantiationMode;
        public int SpecificIndex => specificIndex;

        public List<GameObject> GetSelectedPrefabs()
        {
            if (modelPrefabs == null || modelPrefabs.Count == 0)
            {
                Debug.LogWarning($"No ModelPrefabs assigned in SkinConfigData '{configName}'.", this);
                return new List<GameObject>();
            }

            switch (instantiationMode)
            {
                case InstantiationMode.All:
                    return modelPrefabs;
                case InstantiationMode.First:
                    return new List<GameObject> { modelPrefabs[0] };
                case InstantiationMode.Random:
                    int randomIndex = Random.Range(0, modelPrefabs.Count);
                    return new List<GameObject> { modelPrefabs[randomIndex] };
                case InstantiationMode.Specific:
                    if (specificIndex >= 0 && specificIndex < modelPrefabs.Count)
                    {
                        return new List<GameObject> { modelPrefabs[specificIndex] };
                    }
                    Debug.LogWarning($"Invalid specificIndex {specificIndex} in SkinConfigData '{configName}'. Using first prefab.", this);
                    return new List<GameObject> { modelPrefabs[0] };
                default:
                    return new List<GameObject>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = modelPrefabs.Count - 1; i >= 0; i--)
            {
                if (modelPrefabs[i] != null) continue;
                Debug.LogWarning($"Null ModelPrefab found in SkinConfigData '{configName}' at index {i}. Removing it.", this);
                modelPrefabs.RemoveAt(i);
            }

            if (instantiationMode != InstantiationMode.Specific || (specificIndex >= 0 && specificIndex < modelPrefabs.Count)) return;
            Debug.LogWarning($"Invalid specificIndex {specificIndex} in SkinConfigData '{configName}'. Clamping to valid range.", this);
            specificIndex = Mathf.Clamp(specificIndex, 0, modelPrefabs.Count - 1);
        }
#endif
    }
    public enum InstantiationMode
    {
        All,
        First,
        Random,
        Specific
    }
}