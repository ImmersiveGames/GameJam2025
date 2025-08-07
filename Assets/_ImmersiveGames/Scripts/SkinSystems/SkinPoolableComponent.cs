using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class SkinPoolableComponent : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData skinCollectionData;
        private ContainerManager _containerManager;
        private ModelInstantiator _modelInstantiator;
        private PooledObject _pooledObject;
        private readonly Dictionary<ModelType, List<GameObject>> _modelInstances = new();
        public UnityEvent OnActivated { get; } = new UnityEvent();
        public UnityEvent OnDeactivated { get; } = new UnityEvent();

        private void Awake()
        {
            _containerManager = new ContainerManager();
            _modelInstantiator = new ModelInstantiator();
            _pooledObject = GetComponent<PooledObject>();

            if (_pooledObject != null)
            {
                _pooledObject.OnActivated.AddListener(OnPooledObjectActivated);
                _pooledObject.OnDeactivated.AddListener(OnPooledObjectDeactivated);
            }

            Initialize();
        }

        private void OnDestroy()
        {
            if (_pooledObject != null)
            {
                _pooledObject.OnActivated.RemoveListener(OnPooledObjectActivated);
                _pooledObject.OnDeactivated.RemoveListener(OnPooledObjectDeactivated);
            }
        }

        private void Initialize()
        {
            if (skinCollectionData == null)
            {
                DebugUtility.LogError<SkinPoolableComponent>($"SkinCollectionData is null in '{name}'.", this);
                return;
            }

            _containerManager.CreateContainers(skinCollectionData, transform);
            BuildModels();
        }

        private void BuildModels()
        {
            foreach (ModelType modelType in (ModelType[])Enum.GetValues(typeof(ModelType)))
            {
                Transform container = _containerManager.GetContainer(modelType);
                if (container == null) continue;

                ISkinConfig skinConfig = skinCollectionData.GetConfig(modelType);
                if (skinConfig == null || skinConfig.GetSelectedPrefabs().Count == 0)
                {
                    DebugUtility.LogWarning<SkinPoolableComponent>($"No valid SkinConfig for ModelType '{modelType}' in SkinCollection '{skinCollectionData.CollectionName}'.", this);
                    continue;
                }

                if (!_modelInstances.ContainsKey(modelType))
                {
                    _modelInstances[modelType] = new List<GameObject>();
                }
                else
                {
                    // Reativar modelos existentes
                    foreach (var model in _modelInstances[modelType])
                    {
                        if (model != null)
                        {
                            model.SetActive(true);
                        }
                    }
                    continue;
                }

                _containerManager.ClearContainer(modelType);
                var prefabs = skinConfig.GetSelectedPrefabs();
                foreach (var prefab in prefabs)
                {
                    if (prefab != null)
                    {
                        var instance = _modelInstantiator.InstantiateModel(prefab, container, skinConfig.ConfigName, modelType);
                        _modelInstances[modelType].Add(instance);
                    }
                }
            }
        }

        public void ApplySkin(ISkinConfig newSkin)
        {
            if (newSkin == null || newSkin.GetSelectedPrefabs().Count == 0)
            {
                DebugUtility.LogError<SkinPoolableComponent>($"Invalid SkinConfig provided in '{name}'.", this);
                return;
            }

            Transform container = _containerManager.GetContainer(newSkin.ModelType);
            if (container == null)
            {
                container = _containerManager.CreateContainer(newSkin.ModelType, transform);
            }

            _modelInstances[newSkin.ModelType]?.Clear();
            _modelInstances[newSkin.ModelType] = new List<GameObject>();
            _containerManager.ClearContainer(newSkin.ModelType);

            foreach (var prefab in newSkin.GetSelectedPrefabs())
            {
                if (prefab != null)
                {
                    var instance = _modelInstantiator.InstantiateModel(prefab, container, newSkin.ConfigName, newSkin.ModelType);
                    _modelInstances[newSkin.ModelType].Add(instance);
                }
            }
        }

        public Transform GetContainer(ModelType modelType)
        {
            return _containerManager.GetContainer(modelType);
        }

        public void Activate()
        {
            foreach (var pair in _modelInstances)
            {
                Transform container = _containerManager.GetContainer(pair.Key);
                if (container != null)
                {
                    container.gameObject.SetActive(true);
                    foreach (var model in pair.Value)
                    {
                        if (model != null)
                        {
                            model.SetActive(true);
                            DebugUtility.LogVerbose<SkinPoolableComponent>($"Reativado modelo '{model.name}' em '{name}'.", "green", this);
                        }
                    }
                }
            }
            OnActivated.Invoke();
        }

        public void Deactivate()
        {
            foreach (var pair in _modelInstances)
            {
                Transform container = _containerManager.GetContainer(pair.Key);
                if (container != null)
                {
                    container.gameObject.SetActive(false);
                    DebugUtility.LogVerbose<SkinPoolableComponent>($"Desativado contêiner '{container.name}' em '{name}'.", "blue", this);
                }
            }
            OnDeactivated.Invoke();
        }

        private void OnPooledObjectActivated()
        {
            Activate();
        }

        private void OnPooledObjectDeactivated()
        {
            Deactivate();
        }

        public SkinCollectionData GetSkinCollectionData()
        {
            return skinCollectionData;
        }
    }
}