using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    [CreateAssetMenu(fileName = "SkinConfigData", menuName = "ImmersiveGames/Skin/SkinConfigData", order = 1)]
    public class SkinConfigData : ScriptableObject, ISkinConfig
    {
        [SerializeField] private string configName = "Skin";
        [SerializeField] private ModelType modelType = ModelType.ModelRoot;
        [SerializeField] private List<GameObject> prefabs = new();
        [SerializeField] private InstantiationMode instantiationMode = InstantiationMode.First;
        [SerializeField] private int specificIndex;

        public string ConfigName => configName;
        public ModelType ModelType => modelType;

        public List<GameObject> GetSelectedPrefabs()
        {
            if (prefabs == null || prefabs.Count == 0) return new List<GameObject>();

            return instantiationMode switch
            {
                InstantiationMode.All => prefabs,
                InstantiationMode.First => new List<GameObject> { prefabs[0] },
                InstantiationMode.Random => new List<GameObject> { prefabs[Random.Range(0, prefabs.Count)] },
                InstantiationMode.Specific when specificIndex >= 0 && specificIndex < prefabs.Count =>
                    new List<GameObject> { prefabs[specificIndex] },
                InstantiationMode.Specific => new List<GameObject> { prefabs[0] }, // fallback
                _ => new List<GameObject>()
            };
        }
        
    }
}