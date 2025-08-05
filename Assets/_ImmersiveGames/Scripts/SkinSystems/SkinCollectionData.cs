using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    [CreateAssetMenu(fileName = "SkinCollectionData", menuName = "ImmersiveGames/Skin/SkinCollectionData", order = 3)]
    public class SkinCollectionData : ScriptableObject, ISkinCollection
    {
        [SerializeField] private string collectionName = "Collection Name";
        [SerializeField] private SkinConfigData modelRootConfig;
        [SerializeField] private SkinConfigData canvasRootConfig;
        [SerializeField] private SkinConfigData fxRootConfig;

        public string CollectionName => collectionName;

        public ISkinConfig GetConfig(ModelType modelType)
        {
            switch (modelType)
            {
                case ModelType.ModelRoot: return modelRootConfig;
                case ModelType.CanvasRoot: return canvasRootConfig;
                case ModelType.FxRoot: return fxRootConfig;
                default: return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure modelRootConfig is not null and has the correct ModelType
            if (modelRootConfig == null)
            {
                Debug.LogError($"ModelRootConfig is required in '{collectionName}'.", this);
            }
            else if (modelRootConfig.ModelType != ModelType.ModelRoot)
            {
                Debug.LogWarning($"ModelRootConfig in '{collectionName}' has incorrect ModelType '{modelRootConfig.ModelType}'. Resetting.", this);
                modelRootConfig = null;
            }

            // Validate optional configs
            if (canvasRootConfig != null && canvasRootConfig.ModelType != ModelType.CanvasRoot)
            {
                Debug.LogWarning($"CanvasRootConfig in '{collectionName}' has incorrect ModelType '{canvasRootConfig.ModelType}'. Resetting.", this);
                canvasRootConfig = null;
            }
            if (fxRootConfig != null && fxRootConfig.ModelType != ModelType.FxRoot)
            {
                Debug.LogWarning($"FxRootConfig in '{collectionName}' has incorrect ModelType '{fxRootConfig.ModelType}'. Resetting.", this);
                fxRootConfig = null;
            }
        }
#endif
    }
}