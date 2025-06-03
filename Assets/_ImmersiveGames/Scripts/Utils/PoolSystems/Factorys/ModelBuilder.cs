using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Error)]
    public static class ModelBuilder
    {
        public static GameObject BuildModel(GameObject target, PoolableObjectData data)
        {
            if (!target || !data?.ModelPrefab)
            {
                DebugUtility.LogError(typeof(ModelBuilder), $"Target ou ModelPrefab nulo para '{data?.ObjectName}'.");
                return null;
            }

            Transform modelRootTransform;

            // 🔍 Verifica se já existe um ModelRoot
            var existingModelRoot = target.GetComponentInChildren<ModelRoot>();
            if (existingModelRoot)
            {
                DebugUtility.LogVerbose(typeof(ModelBuilder),$"ModelRoot já existente em '{target.name}', reutilizando.");
                modelRootTransform = existingModelRoot.transform;
            }
            else
            {
                // 🛠 Cria novo ModelRoot
                var modelRootGo = new GameObject("ModelRoot");
                modelRootGo.transform.SetParent(target.transform, false);
                modelRootGo.transform.localPosition = Vector3.zero;
                modelRootGo.transform.localRotation = Quaternion.identity;
                modelRootGo.transform.localScale = Vector3.one;
                modelRootGo.AddComponent<ModelRoot>();

                modelRootTransform = modelRootGo.transform;

                DebugUtility.LogVerbose(typeof(ModelBuilder),$"Criado novo ModelRoot para '{target.name}'.");
            }

            // 🧼 Limpa filhos anteriores (modelos velhos)
            for (int i = modelRootTransform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(modelRootTransform.GetChild(i).gameObject);
            }

            // ✅ Instancia novo modelo como filho do ModelRoot
            var modelInstance = Object.Instantiate(data.ModelPrefab, modelRootTransform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DebugUtility.LogVerbose(typeof(ModelBuilder),$"Novo modelo instanciado dentro do ModelRoot de '{target.name}'.");

            return modelRootTransform.gameObject;
        }
    }
}
