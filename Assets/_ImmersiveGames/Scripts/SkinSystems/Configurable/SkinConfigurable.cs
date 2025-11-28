using System.Collections.Generic;
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
        
        protected ActorSkinController actorSkinController;
        protected IActor ownerActor;
        protected bool isInitialized;

        // Event bindings para eventos globais
        private EventBinding<SkinEvents> _skinUpdateBinding;
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

            actorSkinController = GetComponentInParent<ActorSkinController>();
            ownerActor = GetComponentInParent<IActor>();
            
            if (actorSkinController == null)
            {
                DebugUtility.LogWarning<SkinConfigurable>($"SkinConfigurable: No ActorSkinController found in parent hierarchy of {gameObject.name}");
                return;
            }

            isInitialized = true;
        }

        protected virtual void RegisterWithSkinController()
        {
            if (!isInitialized || actorSkinController == null) return;

            actorSkinController.OnSkinApplied += OnActorSkinAppliedHandler;
            actorSkinController.OnSkinCollectionApplied += OnActorSkinCollectionAppliedHandler;
            actorSkinController.OnSkinInstancesCreated += OnActorSkinInstancesCreatedHandler;
        }

        protected virtual void UnregisterFromSkinController()
        {
            if (actorSkinController != null)
            {
                actorSkinController.OnSkinApplied -= OnActorSkinAppliedHandler;
                actorSkinController.OnSkinCollectionApplied -= OnActorSkinCollectionAppliedHandler;
                actorSkinController.OnSkinInstancesCreated -= OnActorSkinInstancesCreatedHandler;
            }
        }

        protected virtual void RegisterGlobalEvents()
        {
            if (!useGlobalEvents || ownerActor == null) return;

            _skinUpdateBinding = new EventBinding<SkinEvents>(OnGlobalSkinUpdate);
            _skinInstancesBinding = new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);
            
            FilteredEventBus<SkinEvents>.Register(_skinUpdateBinding, ownerActor);
            FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, ownerActor);
        }

        protected virtual void UnregisterGlobalEvents()
        {
            if (ownerActor != null)
            {
                FilteredEventBus<SkinEvents>.Unregister(ownerActor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(ownerActor);
            }
        }

        // Handlers de eventos
        private void OnActorSkinAppliedHandler(ISkinConfig config) => OnSkinApplied(config);
        private void OnActorSkinCollectionAppliedHandler(SkinCollectionData collection) => OnSkinCollectionApplied(collection);
        private void OnActorSkinInstancesCreatedHandler(ModelType type, List<GameObject> instances) => OnSkinInstancesCreated(type, instances);
        private void OnGlobalSkinUpdate(SkinEvents evt) => OnSkinApplied(evt.SkinConfig);
        private void OnGlobalSkinInstancesCreated(SkinInstancesCreatedEvent evt) => OnSkinInstancesCreated(evt.ModelType, new List<GameObject>(evt.Instances));

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

        protected virtual void OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType == targetModelType)
            {
                ConfigureSkinInstances(instances);
            }
        }

        // Métodos abstratos principais
        public abstract void ConfigureSkin(ISkinConfig skinConfig);
        public abstract void ApplyDynamicModifications();
        public virtual void ConfigureSkinInstances(List<GameObject> instances) { }

        // Métodos de utilidade
        protected List<GameObject> GetSkinInstances()
        {
            return actorSkinController?.GetSkinInstances(targetModelType);
        }

        protected Transform GetSkinContainer()
        {
            return actorSkinController?.GetSkinContainer(targetModelType);
        }

        public void ReconfigureCurrentSkin()
        {
            if (actorSkinController != null)
            {
                List<GameObject> instances = GetSkinInstances();
                if (instances is { Count: > 0 })
                {
                    ApplyDynamicModifications();
                }
            }
        }
    }
}