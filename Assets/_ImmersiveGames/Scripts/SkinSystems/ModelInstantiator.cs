using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class ModelInstantiator
    {
        public void InstantiateModels(ISkinConfig skinConfig, Transform container, string collectionName)
        {
            if (container == null)
            {
                Debug.LogError($"No container found for ModelType '{skinConfig.ModelType}' in SkinCollection '{collectionName}'.");
                return;
            }

            foreach (var prefab in skinConfig.GetSelectedPrefabs())
            {
                if (prefab != null)
                {
                    GameObject instantiatedModel = Object.Instantiate(prefab, container);
                    instantiatedModel.name = $"{skinConfig.ConfigName}_{skinConfig.ModelType}";
                    instantiatedModel.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogWarning($"Null prefab found in SkinConfig '{skinConfig.ConfigName}' for ModelType '{skinConfig.ModelType}'.");
                }
            }
        }
    }
}