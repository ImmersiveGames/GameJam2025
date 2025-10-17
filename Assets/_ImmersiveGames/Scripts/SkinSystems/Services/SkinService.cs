using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Serviço responsável por gerenciar contêineres e instâncias de skins.
    /// </summary>
    public class SkinService : ISkinService
    {
        private readonly ContainerService _containerService;
        private readonly ModelFactory _modelFactory;
        private readonly Dictionary<ModelType, List<GameObject>> _instances = new();
        private readonly List<ISkinInstancePostProcessor> _postProcessors = new();
        private IActor _ownerActor;

        public SkinService()
            : this(new ContainerService(), new ModelFactory(), new ISkinInstancePostProcessor[] { new DynamicCanvasBinderPostProcessor() })
        {
        }

        public SkinService(ContainerService containerService, ModelFactory modelFactory, IEnumerable<ISkinInstancePostProcessor> postProcessors)
        {
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _modelFactory = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));

            if (postProcessors != null)
            {
                _postProcessors.AddRange(postProcessors);
            }
        }

        public void Initialize(SkinCollectionData collection, Transform parent, IActor owner)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            _ownerActor = owner;
            _containerService.CreateAllContainers(parent);

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

            var container = _containerService.GetContainer(config.ModelType);
            if (container == null) return Array.Empty<GameObject>();

            ClearInstancesOfType(config.ModelType);

            var prefabs = config.GetSelectedPrefabs()?.Where(prefab => prefab != null).ToList();
            if (prefabs == null || prefabs.Count == 0)
            {
                return Array.Empty<GameObject>();
            }

            var actor = owner ?? _ownerActor;
            var instances = prefabs
                .Select(prefab => _modelFactory.Instantiate(prefab, container, actor, config))
                .Where(instance => instance != null)
                .ToList();

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

        public Transform GetContainer(ModelType type) => _containerService.GetContainer(type);

        private void RunPostProcessors(IEnumerable<GameObject> instances, ISkinConfig config, IActor owner)
        {
            if (!_postProcessors.Any()) return;

            foreach (var instance in instances)
            {
                foreach (var processor in _postProcessors)
                {
                    processor.Process(instance, config, owner);
                }
            }
        }

        private void ClearInstancesOfType(ModelType type)
        {
            if (!_instances.TryGetValue(type, out var instances)) return;

            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            _instances.Remove(type);
        }

        private void ClearAllInstances()
        {
            foreach (var type in _instances.Keys.ToList())
            {
                ClearInstancesOfType(type);
            }

            _instances.Clear();
        }
    }
}
