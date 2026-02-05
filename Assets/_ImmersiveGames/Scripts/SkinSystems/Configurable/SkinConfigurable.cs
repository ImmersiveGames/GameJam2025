using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.SkinSystems.Controllers;
using _ImmersiveGames.Scripts.SkinSystems.Events;

namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public abstract class SkinConfigurable : MonoBehaviour
    {
        [Header("Skin Configurable Settings")]
        [SerializeField] protected ModelType targetModelType = ModelType.ModelRoot;
        [SerializeField] protected bool autoRegister = true;
        [SerializeField] protected bool useGlobalEvents = true;

        private ActorSkinController _actorSkinController;
        private IActor _ownerActor;
        private bool _isSinkinInitialized;
        private bool _isRegistered;

        // Event bindings para eventos globais
        private EventBinding<SkinEvents> _skinUpdateBinding;
        private EventBinding<SkinInstancesCreatedEvent> _skinInstancesBinding;

        protected virtual void Awake()
        {
            Initialize();

            // Importante: registrar no Awake para n�o perder eventos de aplica��o que ocorram cedo.
            if (autoRegister)
            {
                RegisterWithSkinController();
                RegisterGlobalEvents();
            }
        }

        protected virtual void Start()
        {
            // Mantido por compatibilidade: se algo falhar no Awake, tenta garantir no Start.
            // Mas evita double-subscribe.
            if (autoRegister && !_isRegistered)
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
            if (_isSinkinInitialized) return;

            _actorSkinController = GetComponentInParent<ActorSkinController>();
            _ownerActor = GetComponentInParent<IActor>();

            if (_actorSkinController == null)
            {
                DebugUtility.LogWarning<SkinConfigurable>(
                    $"SkinConfigurable: No ActorSkinController found in parent hierarchy of {gameObject.name}");
                return;
            }

            _isSinkinInitialized = true;
        }

        protected virtual void RegisterWithSkinController()
        {
            if (!_isSinkinInitialized || _actorSkinController == null) return;
            if (_isRegistered) return;

            _actorSkinController.OnSkinApplied += OnActorSkinAppliedHandler;
            _actorSkinController.OnSkinCollectionApplied += OnActorSkinCollectionAppliedHandler;
            _actorSkinController.OnSkinInstancesCreated += OnActorSkinInstancesCreatedHandler;

            _isRegistered = true;
        }

        protected virtual void UnregisterFromSkinController()
        {
            if (_actorSkinController != null)
            {
                _actorSkinController.OnSkinApplied -= OnActorSkinAppliedHandler;
                _actorSkinController.OnSkinCollectionApplied -= OnActorSkinCollectionAppliedHandler;
                _actorSkinController.OnSkinInstancesCreated -= OnActorSkinInstancesCreatedHandler;
            }

            _isRegistered = false;
        }

        protected virtual void RegisterGlobalEvents()
        {
            if (!useGlobalEvents || _ownerActor == null) return;

            // Evita recriar bindings se j� existirem
            _skinUpdateBinding ??= new EventBinding<SkinEvents>(OnGlobalSkinUpdate);
            _skinInstancesBinding ??= new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);

            FilteredEventBus<SkinEvents>.Register(_skinUpdateBinding, _ownerActor);
            FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, _ownerActor);
        }

        protected virtual void UnregisterGlobalEvents()
        {
            if (_ownerActor != null)
            {
                FilteredEventBus<SkinEvents>.Unregister(_ownerActor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(_ownerActor);
            }
        }

        // Handlers de eventos
        private void OnActorSkinAppliedHandler(ISkinConfig config) => OnSkinApplied(config);
        private void OnActorSkinCollectionAppliedHandler(SkinCollectionData collection) => OnSkinCollectionApplied(collection);
        private void OnActorSkinInstancesCreatedHandler(ModelType type, List<GameObject> instances) => OnSkinInstancesCreated(type, instances);
        private void OnGlobalSkinUpdate(SkinEvents evt) => OnSkinApplied(evt.SkinConfig);
        private void OnGlobalSkinInstancesCreated(SkinInstancesCreatedEvent evt) => OnSkinInstancesCreated(evt.ModelType, new List<GameObject>(evt.Instances));

        // M�todos para override pelas classes derivadas
        protected virtual void OnSkinApplied(ISkinConfig config)
        {
            if (config != null && config.ModelType == targetModelType)
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

        // M�todos abstratos principais
        protected abstract void ConfigureSkin(ISkinConfig skinConfig);
        protected abstract void ApplyDynamicModifications();
        protected virtual void ConfigureSkinInstances(List<GameObject> instances) { }

        // M�todos de utilidade
        protected List<GameObject> GetSkinInstances()
        {
            return _actorSkinController?.GetSkinInstances(targetModelType);
        }

        protected Transform GetSkinContainer()
        {
            return _actorSkinController?.GetSkinContainer(targetModelType);
        }

        public void ReconfigureCurrentSkin()
        {
            if (_actorSkinController != null)
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

