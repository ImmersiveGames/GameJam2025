using UnityEngine;
using _ImmersiveGames.Scripts.Tags;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class ModelBuilder
    {
        public static void BuildModel(GameObject target, PoolableObjectData data)
        {
            if (target == null || data == null)
            {
                Debug.LogError("BuildModel: Target or data is null");
                return;
            }

            // Criar ModelRoot
            GameObject modelRootGo = new GameObject("ModelRoot");
            modelRootGo.transform.SetParent(target.transform);
            modelRootGo.transform.localPosition = Vector3.zero;
            modelRootGo.AddComponent<ModelRoot>();

            // Instanciar modelo 3D
            if (data.ModelPrefab != null)
            {
                GameObject model = Object.Instantiate(data.ModelPrefab, modelRootGo.transform);
                model.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogWarning($"No modelPrefab assigned in PoolableObjectData '{data.ObjectName}'");
            }
        }
    }
}