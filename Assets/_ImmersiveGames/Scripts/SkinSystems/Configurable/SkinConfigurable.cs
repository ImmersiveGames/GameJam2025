using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public abstract class SkinConfigurable : MonoBehaviour
    {
        [Header("Skin Configurable Settings")]
        [SerializeField] protected ModelType targetModelType = ModelType.ModelRoot;
        [SerializeField] protected bool autoRegister = true;
        [SerializeField] protected bool useGlobalEvents = true;
        
        protected SkinController skinController;
        protected IActor ownerActor;
        protected bool isInitialized = false;

        // Event bindings para eventos globais
        private EventBinding<SkinUpdateEvent> _skinUpdateBinding;
        private EventBinding<SkinInstancesCreatedEvent> _skinInstancesBinding;

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Start()
        {
            if (autoRegister)
            {
                RegisterWithSkinController();
                RegisterGlobalEvents();
            }
        }

        protected virtual void OnDestroy()
        {
            UnregisterFromSkinController();
            UnregisterGlobalEvents();
        }

        protected virtual void Initialize()
        {
            if (isInitialized) return;

            skinController = GetComponentInParent<SkinController>();
            ownerActor = GetComponentInParent<IActor>();
            
            if (skinController == null)
            {
                DebugUtility.LogWarning<SkinConfigurable>($"SkinConfigurable: No SkinController found in parent hierarchy of {gameObject.name}");
                return;
            }

            isInitialized = true;
        }

        protected virtual void RegisterWithSkinController()
        {
            if (!isInitialized || skinController == null) return;

            skinController.OnSkinApplied += OnSkinAppliedHandler;
            skinController.OnSkinCollectionApplied += OnSkinCollectionAppliedHandler;
            skinController.OnSkinInstancesCreated += OnSkinInstancesCreatedHandler;
        }

        protected virtual void UnregisterFromSkinController()
        {
            if (skinController != null)
            {
                skinController.OnSkinApplied -= OnSkinAppliedHandler;
                skinController.OnSkinCollectionApplied -= OnSkinCollectionAppliedHandler;
                skinController.OnSkinInstancesCreated -= OnSkinInstancesCreatedHandler;
            }
        }

        protected virtual void RegisterGlobalEvents()
        {
            if (!useGlobalEvents || ownerActor == null) return;

            _skinUpdateBinding = new EventBinding<SkinUpdateEvent>(OnGlobalSkinUpdate);
            _skinInstancesBinding = new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);
            
            FilteredEventBus<SkinUpdateEvent>.Register(_skinUpdateBinding, ownerActor);
            FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, ownerActor);
        }

        protected virtual void UnregisterGlobalEvents()
        {
            if (ownerActor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.Unregister(ownerActor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(ownerActor);
            }
        }

        // Handlers de eventos
        private void OnSkinAppliedHandler(ISkinConfig config) => OnSkinApplied(config);
        private void OnSkinCollectionAppliedHandler(SkinCollectionData collection) => OnSkinCollectionApplied(collection);
        private void OnSkinInstancesCreatedHandler(ModelType type, System.Collections.Generic.List<GameObject> instances) => OnSkinInstancesCreated(type, instances);
        private void OnGlobalSkinUpdate(SkinUpdateEvent evt) => OnSkinApplied(evt.SkinConfig);
        private void OnGlobalSkinInstancesCreated(SkinInstancesCreatedEvent evt) => OnSkinInstancesCreated(evt.ModelType, new System.Collections.Generic.List<GameObject>(evt.Instances));

        // Métodos para override pelas classes derivadas
        protected virtual void OnSkinApplied(ISkinConfig config)
        {
            if (config.ModelType == targetModelType)
            {
                ConfigureSkin(config);
            }
        }

        protected virtual void OnSkinCollectionApplied(SkinCollectionData collection)
        {
            var config = collection?.GetConfig(targetModelType);
            if (config != null)
            {
                ConfigureSkin(config);
            }
        }

        protected virtual void OnSkinInstancesCreated(ModelType modelType, System.Collections.Generic.List<GameObject> instances)
        {
            if (modelType == targetModelType)
            {
                ConfigureSkinInstances(instances);
            }
        }

        // Métodos abstratos principais
        public abstract void ConfigureSkin(ISkinConfig skinConfig);
        public abstract void ApplyDynamicModifications();
        public virtual void ConfigureSkinInstances(System.Collections.Generic.List<GameObject> instances) { }

        // Métodos de utilidade
        protected System.Collections.Generic.List<GameObject> GetSkinInstances()
        {
            return skinController?.GetSkinInstances(targetModelType);
        }

        protected Transform GetSkinContainer()
        {
            return skinController?.GetSkinContainer(targetModelType);
        }

        public void ReconfigureCurrentSkin()
        {
            if (skinController != null)
            {
                var instances = GetSkinInstances();
                if (instances is { Count: > 0 })
                {
                    ApplyDynamicModifications();
                }
            }
        }
    }
}