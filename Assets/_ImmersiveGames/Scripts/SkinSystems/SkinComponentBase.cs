using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Classe base abstrata para gerenciamento de skins no Unity.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
    public abstract class SkinComponentBase : MonoBehaviour
    {
        #region Fields
        [SerializeField] protected SkinCollectionData skinCollectionData;
        protected ContainerManager containerManager;
        protected ModelInstantiator modelInstantiator;
        protected readonly Dictionary<ModelType, List<GameObject>> modelInstancesByType = new();
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa o componente, configurando dependências.
        /// </summary>
        protected virtual void Awake()
        {
            containerManager = new ContainerManager();
            modelInstantiator = new ModelInstantiator();
        }

        /// <summary>
        /// Inicializa contêineres e modelos com base na coleção de skins.
        /// </summary>
        protected void Initialize()
        {
            if (skinCollectionData == null)
            {
                DebugUtility.LogError<SkinComponentBase>($"SkinCollectionData é nulo em '{name}'.", this);
                return;
            }

            Transform parentTransform = containerManager.CreateContainers(skinCollectionData, transform);
            if (parentTransform == null)
            {
                DebugUtility.LogError<SkinComponentBase>($"Falha ao criar contêineres para '{name}'.", this);
                return;
            }

            BuildModels();
        }

        /// <summary>
        /// Constrói modelos para todos os tipos definidos na coleção de skins.
        /// </summary>
        protected void BuildModels()
        {
            foreach (ModelType modelType in Enum.GetValues(typeof(ModelType)))
            {
                BuildModelForType(modelType);
            }
        }

        /// <summary>
        /// Constrói ou reativa modelos para um tipo específico.
        /// </summary>
        /// <param name="modelType">Tipo do modelo a ser construído.</param>
        protected void BuildModelForType(ModelType modelType)
        {
            Transform container = GetOrCreateContainer(modelType);
            if (container == null) return;

            ISkinConfig skinConfig = skinCollectionData.GetConfig(modelType);
            if (!IsValidSkinConfig(skinConfig, modelType)) return;

            if (TryReuseExistingModels(modelType, out List<GameObject> existingModels))
            {
                ActivateModels(existingModels);
                return;
            }

            InitializeModelInstances(modelType);
            InstantiateModelsForType(skinConfig, container, modelType);
        }
        #endregion

        #region Skin Management
        /// <summary>
        /// Aplica uma nova configuração de skin para o ModelType especificado, limpando e instanciando novos modelos.
        /// </summary>
        /// <param name="newSkin">Configuração de skin a ser aplicada.</param>
        public virtual void ApplySkin(ISkinConfig newSkin)
        {
            if (!IsValidSkinConfig(newSkin, newSkin.ModelType)) return;

            Transform container = GetOrCreateContainer(newSkin.ModelType);
            if (container == null) return;

            InitializeModelInstances(newSkin.ModelType);
            InstantiateModelsForType(newSkin, container, newSkin.ModelType);
        }

        /// <summary>
        /// Obtém o contêiner associado a um tipo de modelo.
        /// </summary>
        /// <param name="modelType">Tipo do modelo.</param>
        /// <returns>Transform do contêiner ou null se não encontrado.</returns>
        public Transform GetContainer(ModelType modelType)
        {
            return containerManager.GetContainer(modelType);
        }

        /// <summary>
        /// Obtém os dados da coleção de skins atual.
        /// </summary>
        /// <returns>Dados da coleção de skins.</returns>
        public SkinCollectionData GetSkinCollectionData()
        {
            return skinCollectionData;
        }
        #endregion

        #region Activation
        /// <summary>
        /// Ativa todos os contêineres e modelos associados.
        /// </summary>
        public virtual void Activate()
        {
            foreach (var (modelType, models) in modelInstancesByType)
            {
                Transform container = containerManager.GetContainer(modelType);
                if (container == null) continue;

                container.gameObject.SetActive(true);
                ActivateModels(models);
            }
            DebugUtility.LogVerbose<SkinComponentBase>($"Ativado SkinComponentBase em '{name}'.", "green", this);
        }

        /// <summary>
        /// Desativa todos os contêineres associados.
        /// </summary>
        public virtual void Deactivate()
        {
            foreach (var (modelType, _) in modelInstancesByType)
            {
                Transform container = containerManager.GetContainer(modelType);
                if (container == null) continue;

                container.gameObject.SetActive(false);
                DebugUtility.LogVerbose<SkinComponentBase>($"Desativado contêiner '{container.name}' em '{name}'.", "blue", this);
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Obtém ou cria um contêiner para o tipo de modelo especificado, garantindo que ele esteja ativo.
        /// </summary>
        /// <param name="modelType">Tipo do modelo.</param>
        /// <returns>Transform do contêiner ou null se não encontrado/criado.</returns>
        protected Transform GetOrCreateContainer(ModelType modelType)
        {
            Transform container = containerManager.GetContainer(modelType);
            if (container == null)
            {
                container = containerManager.CreateContainer(modelType, transform);
                if (container == null)
                {
                    DebugUtility.LogWarning<SkinComponentBase>($"Falha ao criar contêiner para ModelType '{modelType}' em '{name}'.", this);
                    return null;
                }
            }
            container.gameObject.SetActive(true); // Garante que o contêiner esteja ativo
            return container;
        }

        /// <summary>
        /// Valida se a configuração de skin é válida e contém prefabs não nulos.
        /// </summary>
        /// <param name="skinConfig">Configuração de skin a validar.</param>
        /// <param name="modelType">Tipo do modelo associado.</param>
        /// <returns>True se válida, false caso contrário.</returns>
        protected bool IsValidSkinConfig(ISkinConfig skinConfig, ModelType modelType)
        {
            if (skinConfig == null || skinConfig.GetSelectedPrefabs().Count == 0)
            {
                LogInvalidSkinConfig(skinConfig, modelType);
                return false;
            }

            foreach (var prefab in skinConfig.GetSelectedPrefabs())
            {
                if (prefab == null)
                {
                    DebugUtility.LogWarning<SkinComponentBase>($"Prefab nulo encontrado na SkinConfig para ModelType '{modelType}' em '{name}'.", this);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verifica se já existem modelos reutilizáveis para o tipo especificado.
        /// </summary>
        /// <param name="modelType">Tipo do modelo.</param>
        /// <param name="models">Lista de modelos existentes, se houver.</param>
        /// <returns>True se modelos existentes podem ser reutilizados, false caso contrário.</returns>
        protected bool TryReuseExistingModels(ModelType modelType, out List<GameObject> models)
        {
            return modelInstancesByType.TryGetValue(modelType, out models) && models != null && models.Count > 0;
        }

        /// <summary>
        /// Ativa os modelos fornecidos.
        /// </summary>
        /// <param name="models">Lista de modelos a ativar.</param>
        protected void ActivateModels(List<GameObject> models)
        {
            foreach (var model in models)
            {
                if (model != null)
                {
                    model.SetActive(true);
                    DebugUtility.LogVerbose<SkinComponentBase>($"Reativado modelo '{model.name}' em '{name}'.", "green", this);
                }
            }
        }

        /// <summary>
        /// Inicializa ou limpa a lista de instâncias para o tipo de modelo.
        /// </summary>
        /// <param name="modelType">Tipo do modelo.</param>
        protected void InitializeModelInstances(ModelType modelType)
        {
            if (!modelInstancesByType.ContainsKey(modelType))
            {
                modelInstancesByType[modelType] = new List<GameObject>();
            }
            else
            {
                modelInstancesByType[modelType].Clear();
            }
        }

        /// <summary>
        /// Instancia modelos para o tipo especificado no contêiner fornecido e os ativa.
        /// </summary>
        /// <param name="skinConfig">Configuração de skin.</param>
        /// <param name="container">Contêiner onde os modelos serão instanciados.</param>
        /// <param name="modelType">Tipo do modelo.</param>
        protected void InstantiateModelsForType(ISkinConfig skinConfig, Transform container, ModelType modelType)
        {
            containerManager.ClearContainer(modelType);
            List<GameObject> prefabs = skinConfig.GetSelectedPrefabs();
            if (prefabs == null || prefabs.Count == 0)
            {
                DebugUtility.LogWarning<SkinComponentBase>($"Nenhum prefab válido para instanciar em ModelType '{modelType}' em '{name}'.", this);
                return;
            }

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var instance = modelInstantiator.InstantiateModel(prefab, container, skinConfig.ConfigName, modelType);
                    if (instance != null)
                    {
                        instance.SetActive(true); // Ativa o modelo imediatamente após instanciação
                        modelInstancesByType[modelType].Add(instance);
                        DebugUtility.LogVerbose<SkinComponentBase>($"Instanciado modelo '{instance.name}' para ModelType '{modelType}' em '{name}'.", "cyan", this);
                    }
                }
            }
        }

        /// <summary>
        /// Registra mensagens de erro ou aviso para configurações de skin inválidas.
        /// </summary>
        /// <param name="skinConfig">Configuração de skin.</param>
        /// <param name="modelType">Tipo do modelo.</param>
        protected void LogInvalidSkinConfig(ISkinConfig skinConfig, ModelType modelType)
        {
            string message = skinConfig == null
                ? $"Nenhuma SkinConfig para ModelType '{modelType}' em '{name}'."
                : $"Nenhum prefab válido na SkinConfig para ModelType '{modelType}' na coleção '{skinCollectionData?.CollectionName ?? "Unknown"}'.";
            DebugUtility.LogWarning<SkinComponentBase>(message, this);
        }
        #endregion
    }
}