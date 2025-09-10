using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Armazena uma coleção de configurações de skin para diferentes ModelType (ModelRoot, CanvasRoot, FxRoot, SoundRoot).
    /// </summary>
    [CreateAssetMenu(fileName = "SkinCollectionData", menuName = "ImmersiveGames/Skin/SkinCollectionData", order = 3)]
    public class SkinCollectionData : ScriptableObject, ISkinCollection
    {
        [SerializeField] private string collectionName = "Collection Name";
        [SerializeField] private SkinConfigData modelRootConfig;
        [SerializeField] private SkinConfigData canvasRootConfig;
        [SerializeField] private SkinConfigData fxRootConfig;
        [SerializeField] private SkinConfigData soundRootConfig;

        public string CollectionName => collectionName;

        /// <summary>
        /// Obtém a configuração de skin para o ModelType especificado.
        /// </summary>
        /// <param name="modelType">Tipo do modelo (ex: ModelRoot).</param>
        /// <returns>Configuração de skin ou null se não configurada.</returns>
        public ISkinConfig GetConfig(ModelType modelType)
        {
            switch (modelType)
            {
                case ModelType.ModelRoot: return modelRootConfig;
                case ModelType.CanvasRoot: return canvasRootConfig;
                case ModelType.FxRoot: return fxRootConfig;
                case ModelType.SoundRoot: return soundRootConfig;
                default:
                    DebugUtility.LogWarning<SkinCollectionData>($"Unknown ModelType '{modelType}' in '{collectionName}'.", this);
                    return null;
            }
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (modelRootConfig == null || modelRootConfig.ModelType != ModelType.ModelRoot)
            {
                DebugUtility.LogError<SkinCollectionData>($"ModelRootConfig is invalid in '{collectionName}'. Must be set and match ModelType.ModelRoot.", this);
                modelRootConfig = null;
            }

            if (canvasRootConfig != null && canvasRootConfig.ModelType != ModelType.CanvasRoot)
            {
                DebugUtility.LogWarning<SkinCollectionData>($"Invalid CanvasRootConfig in '{collectionName}'. Resetting.", this);
                canvasRootConfig = null;
            }

            if (fxRootConfig != null && fxRootConfig.ModelType != ModelType.FxRoot)
            {
                DebugUtility.LogWarning<SkinCollectionData>($"Invalid FxRootConfig in '{collectionName}'. Resetting.", this);
                fxRootConfig = null;
            }

            if (soundRootConfig != null && soundRootConfig.ModelType != ModelType.SoundRoot)
            {
                DebugUtility.LogWarning<SkinCollectionData>($"Invalid SoundRootConfig in '{collectionName}'. Resetting.", this);
                soundRootConfig = null;
            }
        }
    #endif
    }
}