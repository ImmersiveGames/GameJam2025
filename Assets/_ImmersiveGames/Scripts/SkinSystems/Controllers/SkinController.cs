using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using System;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinController : MonoBehaviour, IResettable
    {
        [Header("Skin Configuration")]
        [SerializeField] private SkinCollectionData defaultSkinCollection;
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableGlobalEvents = true;
        
        private ISkinService _skinService;
        private IActor _ownerActor;
        private IHasSkin _skinOwner;

        // Eventos locais (Service Locator pattern)
        public event Action<ISkinConfig> OnSkinApplied;
        public event Action<SkinCollectionData> OnSkinCollectionApplied;
        public event Action<ModelType, List<GameObject>> OnSkinInstancesCreated;

        // EventBindings para EventBus global
        private EventBinding<SkinUpdateEvent> _skinUpdateBinding;
        private EventBinding<SkinCollectionUpdateEvent> _skinCollectionUpdateBinding;

        // Registro no DependencyManager
        private string _objectId;
        private bool _isRegistered = false;

        public bool IsInitialized { get; private set; }
        public IActor OwnerActor => _ownerActor;

        private void Awake()
        {
            FindDependencies();
            _skinService ??= new SkinService();

            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Permite injeção externa de um serviço de skin customizado (útil para testes).
        /// </summary>
        public void SetSkinService(ISkinService skinService)
        {
            _skinService = skinService ?? throw new ArgumentNullException(nameof(skinService));
        }

        private void Start()
        {
            RegisterWithDependencyManager();
            RegisterGlobalEventListeners();
        }

        private void OnDestroy()
        {
            UnregisterFromDependencyManager();
            UnregisterGlobalEventListeners();
        }

        private void FindDependencies()
        {
            _ownerActor = GetComponentInParent<IActor>();
            _skinOwner = GetComponentInParent<IHasSkin>();

            if (_skinOwner == null)
            {
                Debug.LogError($"SkinController: No IHasSkin implementation found in parent hierarchy of {gameObject.name}");
            }
        }

        #region Dependency Manager Registration
        private void RegisterWithDependencyManager()
        {
            if (_ownerActor == null || string.IsNullOrEmpty(_ownerActor.ActorId)) return;

            _objectId = _ownerActor.ActorId;
            
            try
            {
                DependencyManager.Instance.RegisterForObject(_objectId, this);
                _isRegistered = true;
                
                Debug.Log($"SkinController registered in DependencyManager with ID: {_objectId}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to register SkinController in DependencyManager: {e.Message}");
            }
        }

        private void UnregisterFromDependencyManager()
        {
            if (_isRegistered && !string.IsNullOrEmpty(_objectId))
            {
                DependencyManager.Instance.ClearObjectServices(_objectId);
                _isRegistered = false;
            }
        }
        #endregion

        #region Global Event Bus Integration
        private void RegisterGlobalEventListeners()
        {
            if (!enableGlobalEvents) return;

            // Registrar para escutar eventos globais de skin se necessário
            _skinUpdateBinding = new EventBinding<SkinUpdateEvent>(OnGlobalSkinUpdate);
            _skinCollectionUpdateBinding = new EventBinding<SkinCollectionUpdateEvent>(OnGlobalSkinCollectionUpdate);
            
            // Exemplo: escutar eventos globais filtrados por este actor
            if (_ownerActor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.Register(_skinUpdateBinding, _ownerActor);
                FilteredEventBus<SkinCollectionUpdateEvent>.Register(_skinCollectionUpdateBinding, _ownerActor);
            }
        }

        private void UnregisterGlobalEventListeners()
        {
            if (_ownerActor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.Unregister(_ownerActor);
                FilteredEventBus<SkinCollectionUpdateEvent>.Unregister(_ownerActor);
            }
        }

        private void OnGlobalSkinUpdate(SkinUpdateEvent evt)
        {
            // Reagir a eventos globais de skin update
            // Útil para sincronização em multiplayer ou sistemas cross-actor
        }

        private void OnGlobalSkinCollectionUpdate(SkinCollectionUpdateEvent evt)
        {
            // Reagir a eventos globais de collection update
        }
        #endregion

        public void Initialize()
        {
            if (IsInitialized) return;

            if (_skinOwner?.ModelTransform == null)
            {
                Debug.LogError($"SkinController: No valid ModelTransform found");
                return;
            }

            if (_skinService == null)
            {
                Debug.LogError("SkinController: Nenhum ISkinService configurado.");
                return;
            }

            _skinService.Initialize(defaultSkinCollection, _skinOwner.ModelTransform, _ownerActor);

            IsInitialized = true;
        }

        #region Public API
        public void ApplySkin(ISkinConfig config)
        {
            if (!ValidateInitialization()) return;

            if (config == null)
            {
                Debug.LogWarning("SkinController: Config nula fornecida para ApplySkin.");
                return;
            }

            var createdInstances = _skinService.ApplyConfig(config, _ownerActor);

            NotifySkinApplied(config);
            NotifySkinInstancesCreated(config.ModelType, createdInstances);
        }

        public void ApplySkinCollection(SkinCollectionData newCollection)
        {
            if (!ValidateInitialization()) return;

            var createdByType = _skinService.ApplyCollection(newCollection, _ownerActor);

            NotifySkinCollectionApplied(newCollection);

            foreach (var pair in createdByType)
            {
                NotifySkinInstancesCreated(pair.Key, pair.Value);
            }
        }

        public void SetSkinActive(bool active)
        {
            _skinOwner?.SetSkinActive(active);
        }

        public List<GameObject> GetSkinInstances(ModelType type)
        {
            var instances = _skinService?.GetInstancesOfType(type);
            return ConvertInstances(instances);
        }

        public Transform GetSkinContainer(ModelType type) 
        { 
            return _skinService?.GetContainer(type);
        }

        public bool HasSkinApplied(ModelType type) 
        { 
            return _skinService?.HasInstancesOfType(type) ?? false;
        }

        // IResettable
        public void Reset()
        {
            if (defaultSkinCollection != null)
            {
                ApplySkinCollection(defaultSkinCollection);
            }
            SetSkinActive(true);
        }
        #endregion

        #region Utility Methods
        private void NotifySkinApplied(ISkinConfig config)
        {
            OnSkinApplied?.Invoke(config);

            if (!enableGlobalEvents) return;

            var skinEvent = new SkinUpdateEvent(config, _ownerActor);
            EventBus<SkinUpdateEvent>.Raise(skinEvent);

            if (_ownerActor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.RaiseFiltered(skinEvent, _ownerActor);
            }
        }

        private void NotifySkinCollectionApplied(SkinCollectionData collection)
        {
            OnSkinCollectionApplied?.Invoke(collection);

            if (!enableGlobalEvents) return;

            var collectionEvent = new SkinCollectionUpdateEvent(collection, _ownerActor);
            EventBus<SkinCollectionUpdateEvent>.Raise(collectionEvent);

            if (_ownerActor != null)
            {
                FilteredEventBus<SkinCollectionUpdateEvent>.RaiseFiltered(collectionEvent, _ownerActor);
            }
        }

        private void NotifySkinInstancesCreated(ModelType modelType, IReadOnlyList<GameObject> createdInstances)
        {
            var instances = ConvertInstances(createdInstances);
            if (instances.Count == 0) return;

            OnSkinInstancesCreated?.Invoke(modelType, instances);

            if (!enableGlobalEvents) return;

            var instancesEvent = new SkinInstancesCreatedEvent(modelType, instances.ToArray(), _ownerActor);
            EventBus<SkinInstancesCreatedEvent>.Raise(instancesEvent);

            if (_ownerActor != null)
            {
                FilteredEventBus<SkinInstancesCreatedEvent>.RaiseFiltered(instancesEvent, _ownerActor);
            }
        }

        private static List<GameObject> ConvertInstances(IReadOnlyList<GameObject> instances)
        {
            if (instances == null || instances.Count == 0)
            {
                return new List<GameObject>();
            }

            var result = new List<GameObject>(instances.Count);
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    result.Add(instance);
                }
            }

            return result;
        }

        private bool ValidateInitialization()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"SkinController not initialized. Call Initialize() first.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Obtém componentes específicos das instâncias de skin (útil para AnimationResolver, AudioSystems, etc.)
        /// </summary>
        public List<T> GetComponentsFromSkinInstances<T>(ModelType type) where T : Component
        {
            var components = new List<T>();
            var instances = GetSkinInstances(type);
            
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    components.AddRange(instance.GetComponentsInChildren<T>());
                }
            }
            
            return components;
        }

        /// <summary>
        /// Obtém o primeiro componente específico das instâncias de skin
        /// </summary>
        public T GetComponentFromSkinInstances<T>(ModelType type) where T : Component
        {
            var instances = GetSkinInstances(type);
            
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    var component = instance.GetComponentInChildren<T>();
                    if (component != null) return component;
                }
            }
            
            return null;
        }
        #endregion
    }
}