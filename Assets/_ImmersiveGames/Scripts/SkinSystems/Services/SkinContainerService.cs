using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinContainerService
    {
        private readonly Dictionary<ModelType, Transform> _containers = new();

        public void CreateAllContainers(Transform parent)
        {
            foreach (ModelType type in Enum.GetValues(typeof(ModelType)))
            {
                CreateOrReuseContainer(type, parent);
            }
        }

        public Transform CreateOrReuseContainer(ModelType type, Transform parent)
        {
            if (parent == null) return null;

            string name = type.ToString();
            var container = parent.Find(name);

            if (container == null)
            {
                container = new GameObject(name).transform;
                container.SetParent(parent);
                container.localPosition = Vector3.zero;
                container.localRotation = Quaternion.identity;
                container.localScale = Vector3.one;
            }
            else
            {
                ClearContainer(type);
                container.gameObject.SetActive(true);
            }

            _containers[type] = container;
            return container;
        }

        public void ClearContainer(ModelType type)
        {
            if (!_containers.TryGetValue(type, out var container) || container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(container.GetChild(i).gameObject);
            }
        }

        public Transform GetContainer(ModelType type) => _containers.GetValueOrDefault(type);
    }
}