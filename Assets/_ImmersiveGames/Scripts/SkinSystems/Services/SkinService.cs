using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinService
    {
        private readonly ContainerService _containerService = new();
        private readonly ModelFactory _modelFactory = new();
        private readonly Dictionary<ModelType, List<GameObject>> _instances = new();
        private IActor _ownerActor;

        public void Initialize(SkinCollectionData collection, Transform parent, IActor owner)
        {
            _ownerActor = owner;
            _containerService.CreateAllContainers(parent);
            
            if (collection != null)
            {
                ApplyCollection(collection, owner);
            }
        }

        public void ApplyCollection(SkinCollectionData collection, IActor owner)
        {
            ClearAllInstances();
            
            foreach (ModelType type in System.Enum.GetValues(typeof(ModelType)))
            {
                var config = collection?.GetConfig(type);
                if (config != null)
                {
                    ApplyConfig(config, owner);
                }
            }
        }

        public void ApplyConfig(ISkinConfig config, IActor owner)
        {
            var container = _containerService.GetContainer(config.ModelType);
            if (container == null) return;

            ClearInstancesOfType(config.ModelType);

            var prefabs = config.GetSelectedPrefabs()?.Where(p => p != null).ToList();
            if (prefabs == null || prefabs.Count == 0) return;

            _instances[config.ModelType] = prefabs
                .Select(p => _modelFactory.Instantiate(p, container, owner, config))
                .Where(i => i != null)
                .ToList();
            // 👇 NOVO BLOCO: pós-instanciação, inicializar DynamicCanvasBinder se houver
            foreach (var instance in _instances[config.ModelType])
            {
                if (instance == null) continue;
    
                // Encontrar binders dinâmicos recém-criados
                var dynamicBinders = instance.GetComponentsInChildren<DynamicCanvasBinder>(true);
                foreach (var binder in dynamicBinders)
                {
                    binder.gameObject.SetActive(true); // garantir que está ativo
                    binder.InitializeDynamicCanvas(); // novo método que você adicionará
                }
            }
        }

        private void ClearInstancesOfType(ModelType type)
        {
            if (_instances.ContainsKey(type))
            {
                foreach (var instance in _instances[type])
                {
                    if (instance != null)
                        Object.Destroy(instance);
                }
                _instances[type].Clear();
            }
        }

        private void ClearAllInstances()
        {
            foreach (var type in _instances.Keys.ToList())
            {
                ClearInstancesOfType(type);
            }
            _instances.Clear();
        }

        public List<GameObject> GetInstancesOfType(ModelType type)
        {
            return _instances.TryGetValue(type, out List<GameObject> instance) ? 
                new List<GameObject>(instance) : 
                new List<GameObject>();
        }

        public bool HasInstancesOfType(ModelType type)
        {
            return _instances.ContainsKey(type) && _instances[type] != null && _instances[type].Count > 0;
        }

        public Transform GetContainer(ModelType type) => _containerService.GetContainer(type);

        public IReadOnlyDictionary<ModelType, List<GameObject>> Instances => _instances;
    }
}