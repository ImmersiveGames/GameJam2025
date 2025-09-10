using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Gerencia skins para objetos (estáticos ou poolados), suportando troca coletiva e individual via EventBus.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class SkinController : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData skinCollectionData;

        private readonly Dictionary<ModelType, List<GameObject>> _modelInstancesByType = new();
        private readonly ContainerManager _containerManager = new();
        private readonly ModelInstantiator _modelInstantiator = new();
        private PooledObject _pooledObject;
        private EventBinding<SkinUpdateEvent> _skinUpdateBinding;

        /// <summary>
        /// Inicializa referências, registra eventos e instancia os modelos iniciais.
        /// </summary>
        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
            if (_pooledObject != null)
            {
                var pool = _pooledObject.GetPool;
                if (pool != null)
                {
                    pool.OnObjectActivated.AddListener(OnPooledObjectActivated);
                    pool.OnObjectReturned.AddListener(OnPooledObjectDeactivated);
                    DebugUtility.LogVerbose<SkinController>($"Registrado eventos do pool para '{name}'.", "cyan", this);
                }
                else
                {
                    DebugUtility.LogWarning<SkinController>($"ObjectPool é nulo em PooledObject para '{name}'.", this);
                }
            }

            _skinUpdateBinding = new EventBinding<SkinUpdateEvent>(OnSkinUpdate);
            FilteredEventBus<SkinUpdateEvent>.Register(_skinUpdateBinding, this);

            Initialize();
            if (_modelInstancesByType.Count == 0)
            {
                DebugUtility.LogWarning<SkinController>($"Nenhum modelo inicializado para '{name}' durante Awake.", this);
            }
            else
            {
                DebugUtility.LogVerbose<SkinController>($"Skin inicializada para '{name}' com SkinCollectionData '{skinCollectionData?.CollectionName}'.", "cyan", this);
            }
        }

        /// <summary>
        /// Inicializa contêineres e modelos para todos os ModelType da coleção de skins.
        /// </summary>
        private void Initialize()
        {
            if (skinCollectionData == null)
            {
                DebugUtility.LogError<SkinController>($"SkinCollectionData é nulo em '{name}'.", this);
                return;
            }

            _containerManager.CreateContainers(skinCollectionData, transform);
            ApplySkinCollectionInternal(skinCollectionData, _pooledObject?.Spawner);
        }

        /// <summary>
        /// Aplica uma nova configuração de skin para um ModelType específico.
        /// </summary>
        private void ApplySkinForType(ISkinConfig skinConfig, IActor spawner = null)
        {
            if (skinConfig == null || skinCollectionData == null)
            {
                DebugUtility.LogError<SkinController>($"SkinConfig ou SkinCollectionData inválido em '{name}'.", this);
                return;
            }

            var modelType = skinConfig.ModelType;
            var container = _containerManager.GetContainer(modelType);
            if (container == null)
            {
                DebugUtility.LogWarning<SkinController>($"Nenhum contêiner para ModelType '{modelType}' em '{name}'.", this);
                return;
            }

            var prefabs = skinConfig.GetSelectedPrefabs()?.Where(p => p != null).ToList();
            if (prefabs == null || prefabs.Count == 0)
            {
                DebugUtility.LogWarning<SkinController>($"Nenhum prefab válido para ModelType '{modelType}' em SkinConfigData '{skinConfig.ConfigName}' para '{name}'.", this);
                return;
            }

            // Instancia modelos e atualiza dicionário
            _modelInstancesByType[modelType] = prefabs
                .Select(prefab => _modelInstantiator.InstantiateModel(prefab, container, skinConfig.ConfigName, modelType, spawner))
                .Where(instance => instance != null)
                .ToList();

            if (_modelInstancesByType[modelType].Count > 0)
            {
                DebugUtility.LogVerbose<SkinController>($"Adicionados {_modelInstancesByType[modelType].Count} modelos para ModelType '{modelType}' em '{name}'.", "cyan", this);
            }
            else
            {
                _modelInstancesByType.Remove(modelType);
                DebugUtility.LogWarning<SkinController>($"Nenhum modelo instanciado para ModelType '{modelType}' em '{name}'.", this);
            }
        }

        /// <summary>
        /// Aplica uma nova configuração de skin (individual) e dispara evento filtrado.
        /// </summary>
        public void ApplySkin(ISkinConfig newSkin, IActor spawner = null)
        {
            if (newSkin == null)
            {
                DebugUtility.LogError<SkinController>($"SkinConfig inválido em '{name}'.", this);
                return;
            }

            // Limpa o contêiner antes de aplicar a nova skin
            if (_modelInstancesByType.ContainsKey(newSkin.ModelType))
            {
                _containerManager.ClearContainer(newSkin.ModelType);
                _modelInstancesByType.Remove(newSkin.ModelType);
            }

            ApplySkinForType(newSkin, spawner);
            FilteredEventBus<SkinUpdateEvent>.RaiseFiltered(new SkinUpdateEvent(newSkin, spawner), this);
            DebugUtility.LogVerbose<SkinController>($"Aplicada skin individual '{newSkin.ConfigName}' para ModelType '{newSkin.ModelType}' em '{name}'.", "cyan", this);
        }

        /// <summary>
        /// Aplica uma nova coleção de skins (troca coletiva).
        /// </summary>
        public void ApplySkinCollection(SkinCollectionData newCollection, IActor spawner = null)
        {
            if (newCollection == null)
            {
                DebugUtility.LogError<SkinController>($"SkinCollectionData inválido em '{name}'.", this);
                return;
            }

            skinCollectionData = newCollection;
            _containerManager.CreateContainers(newCollection, transform);
            ApplySkinCollectionInternal(newCollection, spawner);

            if (_modelInstancesByType.Count == 0)
            {
                DebugUtility.LogWarning<SkinController>($"Nenhum modelo inicializado para '{name}' após ApplySkinCollection com SkinCollectionData '{newCollection.CollectionName}'.", this);
            }
            else
            {
                DebugUtility.LogVerbose<SkinController>($"Aplicado SkinCollectionData '{newCollection.CollectionName}' em '{name}' com {_modelInstancesByType.Count} tipos de modelo.", "cyan", this);
            }

            FilteredEventBus<SkinUpdateEvent>.RaiseFiltered(new SkinUpdateEvent(null, spawner), this);
        }

        /// <summary>
        /// Aplica uma coleção de skins para todos os ModelType suportados.
        /// </summary>
        private void ApplySkinCollectionInternal(SkinCollectionData collection, IActor spawner = null)
        {
            if (collection == null)
            {
                DebugUtility.LogError<SkinController>($"SkinCollectionData inválido em '{name}'.", this);
                return;
            }

            // Limpa todos os contêineres e modelos
            foreach (var modelType in Enum.GetValues(typeof(ModelType)).Cast<ModelType>())
            {
                if (_modelInstancesByType.ContainsKey(modelType))
                {
                    _containerManager.ClearContainer(modelType);
                    _modelInstancesByType.Remove(modelType);
                }
            }
            DebugUtility.LogVerbose<SkinController>($"Todos os contêineres limpos para '{name}'.", "blue", this);

            // Aplica skins para cada ModelType com config válida
            foreach (ModelType modelType in Enum.GetValues(typeof(ModelType)))
            {
                var config = collection.GetConfig(modelType);
                if (config != null)
                {
                    ApplySkinForType(config, spawner);
                }
                else
                {
                    DebugUtility.LogVerbose<SkinController>($"Nenhum ISkinConfig para ModelType '{modelType}' em SkinCollectionData '{collection.CollectionName}' para '{name}'.", "yellow", this);
                }
            }
        }

        /// <summary>
        /// Ativa todos os contêineres e modelos associados.
        /// </summary>
        private void Activate()
        {
            foreach (var (modelType, models) in _modelInstancesByType)
            {
                if (_containerManager.GetContainer(modelType) is { } container)
                {
                    container.gameObject.SetActive(true);
                    foreach (var model in models)
                    {
                        if (model != null)
                        {
                            model.SetActive(true);
                        }
                    }
                }
            }
            DebugUtility.LogVerbose<SkinController>($"Modelos ativados para '{name}'.", "green", this);
        }

        /// <summary>
        /// Desativa todos os contêineres associados.
        /// </summary>
        private void Deactivate()
        {
            foreach (var modelType in _modelInstancesByType.Keys)
            {
                if (_containerManager.GetContainer(modelType) is { } container)
                {
                    container.gameObject.SetActive(false);
                }
            }
            DebugUtility.LogVerbose<SkinController>($"Contêineres desativados para '{name}'.", "blue", this);
        }

        /// <summary>
        /// Obtém o contêiner para o ModelType especificado.
        /// </summary>
        public Transform GetContainer(ModelType modelType) => _containerManager.GetContainer(modelType);

        /// <summary>
        /// Obtém os dados da coleção de skins atual.
        /// </summary>
        public SkinCollectionData GetSkinCollectionData() => skinCollectionData;

        /// <summary>
        /// Ativa os modelos ao receber evento do pool.
        /// </summary>
        private void OnPooledObjectActivated(IPoolable poolable)
        {
            if (poolable.GetGameObject() == gameObject)
            {
                DebugUtility.LogVerbose<SkinController>($"Recebido OnPooledObjectActivated para '{name}'.", "green", this);
                Activate();
            }
        }

        /// <summary>
        /// Desativa os modelos ao receber evento do pool.
        /// </summary>
        private void OnPooledObjectDeactivated(IPoolable poolable)
        {
            if (poolable.GetGameObject() == gameObject)
            {
                DebugUtility.LogVerbose<SkinController>($"Recebido OnPooledObjectDeactivated para '{name}'.", "blue", this);
                Deactivate();
            }
        }

        /// <summary>
        /// Processa evento de atualização de skin.
        /// </summary>
        private void OnSkinUpdate(SkinUpdateEvent evt)
        {
            if (evt.Spawner != null && (_pooledObject?.Spawner != evt.Spawner && gameObject.GetComponent<IActor>() != evt.Spawner))
            {
                return;
            }

            if (evt.SkinConfig != null)
            {
                ApplySkinForType(evt.SkinConfig, evt.Spawner);
                DebugUtility.LogVerbose<SkinController>($"Processado SkinUpdateEvent para '{evt.SkinConfig.ConfigName}' em '{name}'.", "cyan", this);
            }
        }

        /// <summary>
        /// Remove bindings de eventos ao destruir o objeto.
        /// </summary>
        private void OnDestroy()
        {
            if (_pooledObject?.GetPool != null)
            {
                _pooledObject.GetPool.OnObjectActivated.RemoveListener(OnPooledObjectActivated);
                _pooledObject.GetPool.OnObjectReturned.RemoveListener(OnPooledObjectDeactivated);
            }
            FilteredEventBus<SkinUpdateEvent>.Unregister(this);
            DebugUtility.LogVerbose<SkinController>($"Destruído SkinController em '{name}'.", "blue", this);
        }
    }
}