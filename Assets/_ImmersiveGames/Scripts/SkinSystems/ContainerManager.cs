using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class ContainerManager
    {
        private readonly Dictionary<ModelType, Transform> _containers = new();

        public Transform CreateContainers(ISkinCollection skinCollection, Transform logicTransform)
        {
            if (skinCollection == null)
            {
                Debug.LogError($"SkinCollection is null in ContainerManager for parent '{logicTransform.name}'.");
                return null;
            }

            // Desativar contêineres existentes em vez de destruí-los
            foreach (Transform child in logicTransform)
            {
                if (child.name == ModelType.ModelRoot.ToString() ||
                    child.name == ModelType.CanvasRoot.ToString() ||
                    child.name == ModelType.FxRoot.ToString())
                {
                    child.gameObject.SetActive(false);
                }
            }

            CreateContainer(ModelType.ModelRoot, logicTransform);
            if (skinCollection.GetConfig(ModelType.CanvasRoot) != null)
                CreateContainer(ModelType.CanvasRoot, logicTransform);
            if (skinCollection.GetConfig(ModelType.FxRoot) != null)
                CreateContainer(ModelType.FxRoot, logicTransform);

            return logicTransform;
        }

        public Transform CreateContainer(ModelType modelType, Transform logicTransform)
        {
            string containerName = modelType.ToString();
            Transform container = logicTransform.Find(containerName);
            if (container != null)
            {
                // Limpar filhos, mas manter o contêiner
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(container.GetChild(i).gameObject);
                }
                container.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log($"Creating new container for ModelType '{modelType}' in '{logicTransform.name}'.");
                container = new GameObject(containerName).transform;
                container.SetParent(logicTransform);
                container.localPosition = Vector3.zero;
                container.localRotation = Quaternion.identity;
            }
            _containers[modelType] = container;
            return container;
        }

        public void ClearContainer(ModelType modelType)
        {
            if (_containers.TryGetValue(modelType, out Transform container) && container != null)
            {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(container.GetChild(i).gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"No container found for ModelType '{modelType}' to clear.");
            }
        }

        public Transform GetContainer(ModelType modelType)
        {
            _containers.TryGetValue(modelType, out Transform container);
            return container;
        }
    }
}