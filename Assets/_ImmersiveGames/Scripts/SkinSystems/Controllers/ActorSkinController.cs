using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class ActorSkinController : MonoBehaviour, IResettable
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
        private EventBinding<SkinEvents> _skinUpdateBinding;
        private EventBinding<SkinCollectionUpdateEvent> _skinCollectionUpdateBinding;
        private bool _globalEventsRegistered;

        // Registro no DependencyManager
        private string _objectId;
        private bool _isRegistered;

        private bool IsInitialized { get; set; }
        public IActor OwnerActor => _ownerActor;

        private void Awake()
        {
            FindDependencies();
            _skinService ??= new DefaultSkinService();

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
        }

        private void OnEnable()
        {
            RegisterGlobalEventListeners();
        }

        private void OnDisable()
        {
            UnregisterGlobalEventListeners();
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
                DebugUtility.LogError<ActorSkinController>($"ActorSkinController: No IHasSkin implementation found in parent hierarchy of {gameObject.name}");
            }
        }

        #region Dependency Manager Registration
        private void RegisterWithDependencyManager()
        {
            if (_ownerActor == null || string.IsNullOrEmpty(_ownerActor.ActorId)) return;

            _objectId = _ownerActor.ActorId;
            
            try
            {
                DependencyManager.Provider.RegisterForObject(_objectId, this);
                _isRegistered = true;
                
                DebugUtility.LogVerbose<ActorSkinController>($"ActorSkinController registered in DependencyManager with ID: {_objectId}");
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning<ActorSkinController>($"Failed to register ActorSkinController in DependencyManager: {e.Message}");
            }
        }

        private void UnregisterFromDependencyManager()
        {
            if (_isRegistered && !string.IsNullOrEmpty(_objectId))
            {
                DependencyManager.Provider.ClearObjectServices(_objectId);
                _isRegistered = false;
            }
        }
        #endregion

        #region Global Event Bus Integration
        private void RegisterGlobalEventListeners()
        {
            if (!enableGlobalEvents || _globalEventsRegistered) return;

            _skinUpdateBinding ??= new EventBinding<SkinEvents>(OnGlobalSkinUpdate);
            _skinCollectionUpdateBinding ??= new EventBinding<SkinCollectionUpdateEvent>(OnGlobalSkinCollectionUpdate);

            if (_ownerActor != null)
            {
                FilteredEventBus<SkinEvents>.Register(_skinUpdateBinding, _ownerActor);
                FilteredEventBus<SkinCollectionUpdateEvent>.Register(_skinCollectionUpdateBinding, _ownerActor);
                _globalEventsRegistered = true;
            }
        }

        private void UnregisterGlobalEventListeners()
        {
            if (!_globalEventsRegistered || _ownerActor == null) return;

            if (_skinUpdateBinding != null)
            {
                FilteredEventBus<SkinEvents>.Unregister(_skinUpdateBinding, _ownerActor);
            }

            if (_skinCollectionUpdateBinding != null)
            {
                FilteredEventBus<SkinCollectionUpdateEvent>.Unregister(_skinCollectionUpdateBinding, _ownerActor);
            }

            _globalEventsRegistered = false;
        }

        private void OnGlobalSkinUpdate(SkinEvents evt)
        {
            // Reagir a eventos globais de skin update
            // Útil para sincronização em multiplayer ou sistemas cross-actor
        }

        private void OnGlobalSkinCollectionUpdate(SkinCollectionUpdateEvent evt)
        {
            // Reagir a eventos globais de collection update
        }
        #endregion

        private void Initialize()
        {
            if (IsInitialized) return;

            if (_skinOwner?.ModelTransform == null)
            {
                DebugUtility.LogError<ActorSkinController>($"ActorSkinController: No valid ModelTransform found");
                return;
            }

            if (_skinService == null)
            {
                DebugUtility.LogError<ActorSkinController>("ActorSkinController: Nenhum ISkinService configurado.");
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
                DebugUtility.LogWarning<ActorSkinController>("ActorSkinController: Config nula fornecida para ApplySkin.");
                return;
            }

            IReadOnlyList<GameObject> createdInstances = _skinService.ApplyConfig(config, _ownerActor);

            NotifySkinApplied(config);
            NotifySkinInstancesCreated(config.ModelType, createdInstances);
        }

        private void ApplySkinCollection(SkinCollectionData newCollection)
        {
            if (!ValidateInitialization()) return;

            IReadOnlyDictionary<ModelType, IReadOnlyList<GameObject>> createdByType = _skinService.ApplyCollection(newCollection, _ownerActor);

            NotifySkinCollectionApplied(newCollection);

            foreach (KeyValuePair<ModelType, IReadOnlyList<GameObject>> pair in createdByType)
            {
                NotifySkinInstancesCreated(pair.Key, pair.Value);
            }
        }

        private void SetSkinActive(bool active)
        {
            _skinOwner?.SetSkinActive(active);
        }

        public List<GameObject> GetSkinInstances(ModelType type)
        {
            IReadOnlyList<GameObject> instances = _skinService?.GetInstancesOfType(type);
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

            var skinEvent = new SkinEvents(config, _ownerActor);
            EventBus<SkinEvents>.Raise(skinEvent);

            if (_ownerActor != null)
            {
                FilteredEventBus<SkinEvents>.RaiseFiltered(skinEvent, _ownerActor);
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
            List<GameObject> instances = ConvertInstances(createdInstances);
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
                DebugUtility.LogWarning<ActorSkinController>($"ActorSkinController not initialized. Call Initialize() first.");
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
            List<GameObject> instances = GetSkinInstances(type);
            
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
            List<GameObject> instances = GetSkinInstances(type);
            
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

        /// <summary>
        /// Helper para acessar o SkinRuntimeState diretamente a partir do controller.
        /// </summary>
        public bool TryGetRuntimeState(ModelType type, out SkinRuntimeState state)
        {
            var tracker = GetComponent<SkinRuntimeStateTracker>();
            if (tracker == null)
            {
                state = default;
                return false;
            }

            return tracker.TryGetState(type, out state);
        }
        #endregion
        
#if UNITY_EDITOR
        [ContextMenu("Log Skin Runtime States")]
        private void Editor_LogSkinRuntimeStates()
        {
            var tracker = GetComponent<SkinRuntimeStateTracker>();
            if (tracker == null)
            {
                DebugUtility.LogWarning<ActorSkinController>(
                    $"ActorSkinController em {name} não encontrou SkinRuntimeStateTracker no mesmo GameObject.");
                return;
            }

            tracker.LogAllStatesToConsole();
        }
#endif
    }
}
