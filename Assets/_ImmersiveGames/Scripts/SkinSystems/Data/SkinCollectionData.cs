using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    public interface ISkinCollection
    {
        string CollectionName { get; }
        ISkinConfig GetConfig(ModelType modelType);
    }
    [CreateAssetMenu(fileName = "SkinCollectionData", menuName = "ImmersiveGames/Skin/SkinCollectionData", order = 2)]
    public class SkinCollectionData : ScriptableObject, ISkinCollection
    {
        [SerializeField] private string collectionName = "Collection Name";
        [SerializeField] private List<SkinConfigData> configs = new();

        private Dictionary<ModelType, ISkinConfig> _configMap;

        public string CollectionName => collectionName;

        public ISkinConfig GetConfig(ModelType type)
        {
            EnsureMap();
            return _configMap.GetValueOrDefault(type);
        }

        private void EnsureMap()
        {
            if (_configMap != null) return;
            _configMap = configs
                .Where(c => c != null)
                .GroupBy(c => c.ModelType)
                .ToDictionary(g => g.Key, g => g.First() as ISkinConfig);
        }
    }
}