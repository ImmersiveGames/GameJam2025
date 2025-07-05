using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.EventsBus;
using UnityEngine;
using _ImmersiveGames.Scripts.SkinSystems.Handlers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.SelectorGeneric;

namespace _ImmersiveGames.Scripts.SkinSystems.Loaders
{
    public class SkinModelLoader
    {
        private readonly Transform _parent;
        private readonly Dictionary<ObjectRootType, IRootHandler> _rootHandlers = new();
        
        private readonly Dictionary<string, InstantiatedObjectInfo> _taggedInstances = new();
        private readonly List<InstantiatedObjectInfo> _allInstances = new();

        public IReadOnlyList<InstantiatedObjectInfo> AllInstances => _allInstances;
        public bool TryGetInstanceByTag(string tag, out InstantiatedObjectInfo info) => _taggedInstances.TryGetValue(tag, out info);


        public SkinModelLoader(Transform parent)
        {
            _parent = parent;
        }

        public void Load(List<GameObjectSelectorData> selectorDataList, int instanceId)
        {
            foreach (var data in selectorDataList)
            {
                var rootType = data.ObjectRootType;

                if (!_rootHandlers.TryGetValue(rootType, out var handler))
                {
                    handler = RootHandlerFactory.Create(rootType);
                    _rootHandlers[rootType] = handler;
                }

                var root = handler.GetOrCreateRoot(_parent);
                handler.Clear();
                _allInstances.Clear();
                _taggedInstances.Clear();

                int i = 0;
                foreach (var (prefab, isActive, tag) in data.Select(instanceId))
                {
                    if (prefab == null) continue;

                    var go = Object.Instantiate(prefab, root, false);
                    go.name = !string.IsNullOrEmpty(tag) ? tag : $"{data.SelectorName}_{i++}";
                    go.transform.localPosition = data.PositionOffset;
                    go.transform.localRotation = data.RotationOffset;
                    go.transform.localScale = data.ScaleOffset;
                    go.SetActive(isActive);

                    var info = new InstantiatedObjectInfo(go, data.ObjectRootType, tag);
                    _allInstances.Add(info);
                    if (!string.IsNullOrEmpty(tag))
                        _taggedInstances[tag] = info;

                    DebugUtility.LogVerbose<SkinModelLoader>($"Instanciado '{go.name}' em '{root.name}' (ativo: {isActive})");
                }

                handler.SetActive(true);
            }
        }
        
        public void SetActive(string tag, bool active)
        {
            if (!_taggedInstances.TryGetValue(tag, out var info)) return;
            info.GameObject.SetActive(active);
            EventBus<SkinElementToggledEvent>.Raise(
                new SkinElementToggledEvent(tag, info.GameObject, active));
        }

        public void ClearAll()
        {
            foreach (var instance in _allInstances)
            {
                Object.Destroy(instance.GameObject);
            }

            _allInstances.Clear();
            _taggedInstances.Clear();
        }
    }
}