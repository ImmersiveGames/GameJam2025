using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Serviço responsável por gerenciar contêineres e instâncias de skins.
    /// </summary>
    public class DefaultSkinService : ISkinService
    {
        private readonly SkinContainerService _skinContainerService;
        private readonly SkinModelFactory _skinModelFactory;
        private readonly Dictionary<ModelType, List<GameObject>> _instances = new();
        private readonly List<ISkinInstancePostProcessor> _postProcessors = new();
        private IActor _ownerActor;

        public DefaultSkinService()
            : this(new SkinContainerService(), new SkinModelFactory(), new ISkinInstancePostProcessor[] { new DynamicCanvasBinderPostProcessor() })
        {
        }

        private DefaultSkinService(SkinContainerService skinContainerService, SkinModelFactory skinModelFactory, IEnumerable<ISkinInstancePostProcessor> postProcessors)
        {
            _skinContainerService = skinContainerService ?? throw new ArgumentNullException(nameof(skinContainerService));
            _skinModelFactory = skinModelFactory ?? throw new ArgumentNullException(nameof(skinModelFactory));

            if (postProcessors != null)
            {
                foreach (var processor in postProcessors)
                {
                    if (processor != null)
                    {
                        _postProcessors.Add(processor);
                    }
                }
            }

            EnsureDefaultPostProcessor();
        }

        public void Initialize(SkinCollectionData collection, Transform parent, IActor owner)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            _ownerActor = owner;
            _skinContainerService.CreateAllContainers(parent);

            if (collection != null)
            {
                ApplyCollection(collection, owner);
            }
        }

        public IReadOnlyDictionary<ModelType, IReadOnlyList<GameObject>> ApplyCollection(SkinCollectionData collection, IActor owner)
        {
            ClearAllInstances();

            var createdInstances = new Dictionary<ModelType, IReadOnlyList<GameObject>>();
            if (collection == null) return createdInstances;

            foreach (var config in collection.GetAllConfigs())
            {
                var instances = ApplyConfig(config, owner);
                if (instances.Count > 0)
                {
                    createdInstances[config.ModelType] = instances;
                }
            }

            return createdInstances;
        }

        public IReadOnlyList<GameObject> ApplyConfig(ISkinConfig config, IActor owner)
        {
            if (config == null) return Array.Empty<GameObject>();

            var container = _skinContainerService.GetContainer(config.ModelType);
            if (container == null) return Array.Empty<GameObject>();

            ClearInstancesOfType(config.ModelType);

            var prefabs = config.GetSelectedPrefabs();
            if (prefabs == null || prefabs.Count == 0)
            {
                return Array.Empty<GameObject>();
            }

            var actor = owner ?? _ownerActor;
            var instances = new List<GameObject>(prefabs.Count);
            instances.AddRange(from prefab in prefabs where prefab != null select _skinModelFactory.Instantiate(prefab, container, actor, config) into instance where instance != null select instance);

            if (instances.Count == 0)
            {
                return Array.Empty<GameObject>();
            }

            _instances[config.ModelType] = instances;
            RunPostProcessors(instances, config, actor);

            return instances;
        }

        public IReadOnlyList<GameObject> GetInstancesOfType(ModelType type)
        {
            return _instances.TryGetValue(type, out var instanceList)
                ? instanceList
                : Array.Empty<GameObject>();
        }

        public bool HasInstancesOfType(ModelType type)
        {
            return _instances.TryGetValue(type, out var instanceList) && instanceList.Count > 0;
        }

        public Transform GetContainer(ModelType type) => _skinContainerService.GetContainer(type);

        private void RunPostProcessors(IEnumerable<GameObject> instances, ISkinConfig config, IActor owner)
        {
            if (_postProcessors.Count == 0) return;

            foreach (var instance in instances)
            {
                foreach (var processor in _postProcessors)
                {
                    processor.Process(instance, config, owner);
                }
            }
        }

        private void EnsureDefaultPostProcessor()
        {
            if (_postProcessors.OfType<DynamicCanvasBinderPostProcessor>().Any())
            {
                return;
            }

            _postProcessors.Add(new DynamicCanvasBinderPostProcessor());
        }

        private void ClearInstancesOfType(ModelType type)
        {
            if (!_instances.TryGetValue(type, out var instances)) return;

            foreach (var instance in instances.Where(instance => instance != null))
            {
                Object.Destroy(instance);
            }

            _instances.Remove(type);
        }

        private void ClearAllInstances()
        {
            if (_instances.Count == 0)
            {
                return;
            }

            var keys = new ModelType[_instances.Count];
            _instances.Keys.CopyTo(keys, 0);

            foreach (var t in keys)
            {
                ClearInstancesOfType(t);
            }

            _instances.Clear();
        }
    }
}
