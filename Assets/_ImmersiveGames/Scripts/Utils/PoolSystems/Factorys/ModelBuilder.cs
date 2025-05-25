using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class ModelBuilder
    {
        public static GameObject BuildModel(GameObject target, PoolableObjectData data)
        {
            if (!target || !data?.ModelPrefab)
            {
                DebugUtility.LogError(typeof(ModelBuilder), $"Target ou ModelPrefab nulo para '{data?.ObjectName}'.");
                return null;
            }

            var modelRootGo = new GameObject("ModelRoot");
            modelRootGo.transform.SetParent(target.transform, false);
            modelRootGo.transform.localPosition = Vector3.zero;
            modelRootGo.AddComponent<ModelRoot>();

            var model = Object.Instantiate(data.ModelPrefab, modelRootGo.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            return modelRootGo;
        }
    }
}