using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class ModelInstantiator
    {
        public GameObject InstantiateModel(GameObject prefab, Transform container, string configName, ModelType modelType)
        {
            if (container == null)
            {
                Debug.LogError($"No container found for ModelType '{modelType}' in SkinConfig '{configName}'.");
                return null;
            }

            if (prefab != null)
            {
                GameObject instantiatedModel = Object.Instantiate(prefab, container);
                instantiatedModel.name = $"{configName}_{modelType}";
                instantiatedModel.transform.localPosition = Vector3.zero;
                instantiatedModel.transform.localRotation = Quaternion.identity;
                return instantiatedModel;
            }

            Debug.LogWarning($"Null prefab found in SkinConfig '{configName}' for ModelType '{modelType}'.");
            return null;
        }
    }
}