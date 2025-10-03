using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinService
    {
        private readonly ContainerService _containerService = new();
        private readonly ModelFactory _modelFactory = new();
        private readonly Dictionary<ModelType, List<GameObject>> _instances = new();

        public void Initialize(SkinCollectionData collection, Transform parent, IActor spawner)
        {
            _containerService.CreateAllContainers(parent);
            ApplyCollection(collection, spawner);
        }

        public void ApplyCollection(SkinCollectionData collection, IActor spawner)
        {
            _instances.Clear();
            foreach (ModelType type in System.Enum.GetValues(typeof(ModelType)))
            {
                var config = collection.GetConfig(type);
                if (config != null)
                {
                    ApplyConfig(config, spawner);
                }
            }
        }

        public void ApplyConfig(ISkinConfig config, IActor spawner)
        {
            var container = _containerService.GetContainer(config.ModelType);
            if (container == null) return;

            var prefabs = config.GetSelectedPrefabs()?.Where(p => p != null).ToList();
            if (prefabs == null || prefabs.Count == 0) return;

            _instances[config.ModelType] = prefabs
                .Select(p => _modelFactory.Instantiate(p, container, config.ConfigName, config.ModelType, spawner))
                .Where(i => i != null)
                .ToList();
        }

        public Transform GetContainer(ModelType type) => _containerService.GetContainer(type);
        public IReadOnlyDictionary<ModelType, List<GameObject>> Instances => _instances;
    }
}