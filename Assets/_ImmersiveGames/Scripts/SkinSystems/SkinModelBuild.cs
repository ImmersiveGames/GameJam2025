using System;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinModelBuild
    {
        private readonly ISkinCollection _skinCollection;
        private readonly ContainerManager _containerManager;
        private readonly ModelInstantiator _modelInstantiator;

        public SkinModelBuild(ISkinCollection skinCollection, Transform logicTransform)
        {
            if (skinCollection == null)
            {
                Debug.LogError($"SkinCollection is null in SkinModelBuild for parent '{logicTransform.name}'.");
                return;
            }

            _skinCollection = skinCollection;
            _containerManager = new ContainerManager();
            _modelInstantiator = new ModelInstantiator();
            _containerManager.CreateContainers(skinCollection, logicTransform);
            BuildModels();
        }

        private void BuildModels()
        {
            if (_skinCollection == null)
            {
                Debug.LogError($"SkinCollection is null in SkinModelBuild. Cannot build models.");
                return;
            }

            foreach (ModelType modelType in (ModelType[])Enum.GetValues(typeof(ModelType)))
            {
                Transform container = _containerManager.GetContainer(modelType);
                if (container == null) continue;

                ISkinConfig skinConfig = _skinCollection.GetConfig(modelType);
                if (skinConfig == null || skinConfig.GetSelectedPrefabs().Count == 0)
                {
                    Debug.LogWarning($"No valid SkinConfig for ModelType '{modelType}' in SkinCollection '{_skinCollection.CollectionName}'.");
                    continue;
                }

                _containerManager.ClearContainer(modelType);
                _modelInstantiator.InstantiateModels(skinConfig, container, _skinCollection.CollectionName);
            }
        }

        public void ApplySkin(ISkinConfig newSkin)
        {
            if (newSkin == null || newSkin.GetSelectedPrefabs().Count == 0)
            {
                Debug.LogError($"Invalid SkinConfig provided in SkinModelBuild for SkinCollection '{_skinCollection?.CollectionName}'.");
                return;
            }

            Transform parent = _containerManager.GetContainer(ModelType.ModelRoot)?.parent;
            if (!_containerManager.GetContainer(newSkin.ModelType))
            {
                _containerManager.CreateContainer(newSkin.ModelType, parent);
            }

            _containerManager.ClearContainer(newSkin.ModelType);
            _modelInstantiator.InstantiateModels(newSkin, _containerManager.GetContainer(newSkin.ModelType), _skinCollection.CollectionName);
        }

        public Transform GetContainer(ModelType modelType)
        {
            return _containerManager.GetContainer(modelType);
        }
    }
}