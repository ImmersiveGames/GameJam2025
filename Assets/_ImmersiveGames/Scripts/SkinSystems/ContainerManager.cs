using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Gerencia a criação e manutenção de contêineres hierárquicos (ModelRoot, CanvasRoot, FxRoot, SoundRoot) para objetos.
    /// </summary>
    [DebugLevel(DebugLevel.Error)]
    public class ContainerManager
    {
        private readonly Dictionary<ModelType, Transform> _containers = new();

        /// <summary>
        /// Cria ou reativa contêineres para todos os ModelType, mesmo que as configurações sejam opcionais.
        /// </summary>
        /// <param name="skinCollection">Coleção de configurações de skin.</param>
        /// <param name="logicTransform">Transform pai dos contêineres.</param>
        /// <returns>Transform pai ou null se inválido.</returns>
        public void CreateContainers(ISkinCollection skinCollection, Transform logicTransform)
        {
            if (skinCollection == null || logicTransform == null)
            {
                DebugUtility.LogError<ContainerManager>($"SkinCollection or logicTransform is null.");
                return;
            }

            // Desativa contêineres existentes
            foreach (Transform child in logicTransform)
            {
                if (Enum.TryParse(child.name, out ModelType modelType) && _containers.ContainsKey(modelType))
                {
                    child.gameObject.SetActive(false);
                }
            }

            // Cria contêineres para todos os ModelType
            foreach (ModelType modelType in Enum.GetValues(typeof(ModelType)))
            {
                CreateContainer(modelType, logicTransform);
            }

            DebugUtility.LogVerbose<ContainerManager>($"Containers created for '{logicTransform.name}'.", "cyan");
        }

        /// <summary>
        /// Cria ou reutiliza um contêiner para o ModelType especificado.
        /// </summary>
        /// <param name="modelType">Tipo do contêiner (ex: ModelRoot).</param>
        /// <param name="logicTransform">Transform pai.</param>
        /// <returns>Transform do contêiner ou null se inválido.</returns>
        private void CreateContainer(ModelType modelType, Transform logicTransform)
        {
            if (logicTransform == null)
            {
                DebugUtility.LogError<ContainerManager>($"logicTransform is null for ModelType '{modelType}'.");
                return;
            }

            string containerName = modelType.ToString();
            var container = logicTransform.Find(containerName);
            if (container != null)
            {
                ClearContainer(modelType);
                container.gameObject.SetActive(true);
            }
            else
            {
                container = new GameObject(containerName).transform;
                container.SetParent(logicTransform);
                container.localPosition = Vector3.zero;
                container.localRotation = Quaternion.identity;
                DebugUtility.LogVerbose<ContainerManager>($"Created container '{containerName}' for '{logicTransform.name}'.", "cyan");
            }

            _containers[modelType] = container;
        }

        /// <summary>
        /// Limpa todos os filhos de um contêiner, mantendo o contêiner.
        /// </summary>
        /// <param name="modelType">Tipo do contêiner a limpar.</param>
        public void ClearContainer(ModelType modelType)
        {
            if (!_containers.TryGetValue(modelType, out var container) || container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(container.GetChild(i).gameObject);
            }
            DebugUtility.LogVerbose<ContainerManager>($"Cleared container '{modelType}'.", "blue");
        }

        /// <summary>
        /// Obtém o contêiner para o ModelType especificado.
        /// </summary>
        /// <param name="modelType">Tipo do contêiner.</param>
        /// <returns>Transform do contêiner ou null se não encontrado.</returns>
        public Transform GetContainer(ModelType modelType)
        {
            if (_containers.TryGetValue(modelType, out Transform container))
            {
                return container;
            }
            DebugUtility.LogWarning<ContainerManager>($"No container found for ModelType '{modelType}'.");
            return null;
        }
    }
    /// <summary>
    /// Instancia modelos (prefabs) em contêineres, usando instanciação direta para todos os ModelType.
    /// </summary>
    
    [DebugLevel(DebugLevel.Error)]
    public class ModelInstantiator
    {
        /// <summary>
        /// Instancia um modelo no contêiner especificado.
        /// </summary>
        /// <param name="prefab">Prefab a instanciar.</param>
        /// <param name="container">Contêiner pai.</param>
        /// <param name="configName">Nome da configuração de skin.</param>
        /// <param name="modelType">Tipo do modelo (ex: ModelRoot).</param>
        /// <param name="spawner"></param>
        /// <returns>GameObject instanciado ou null se falhar.</returns>
        public GameObject InstantiateModel(GameObject prefab, Transform container, string configName, ModelType modelType, IActor spawner)
        {
            if (container == null)
            {
                DebugUtility.LogError<ModelInstantiator>($"No container for ModelType '{modelType}' in SkinConfig '{configName}'.");
                return null;
            }

            if (prefab == null)
            {
                DebugUtility.LogWarning<ModelInstantiator>($"Null prefab in SkinConfig '{configName}' for ModelType '{modelType}'.");
                return null;
            }

            var instance = Object.Instantiate(prefab, container);
            instance.name = $"{configName}_{modelType}";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            DebugUtility.LogVerbose<ModelInstantiator>($"Instantiated model '{instance.name}' for '{configName}'.", "cyan");
            return instance;
        }
    }
}