using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.SkinSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.AnimationSystems.Components
{
    [DefaultExecutionOrder(-50)]
    
    public class AnimationResolver : MonoBehaviour, IAnimatorProvider
    {
        private Animator _cachedAnimator;
        private IActor _actor;
        private SkinController _skinController;
        
        // Event bindings
        private EventBinding<SkinUpdateEvent> _skinUpdateBinding;
        private EventBinding<SkinInstancesCreatedEvent> _skinInstancesBinding;

        public event System.Action<Animator> OnAnimatorChanged;

        public Animator GetAnimator() => _cachedAnimator ??= ResolveAnimator();

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            InitializeDependencyRegistration();
        }

        private void Start()
        {
            FindSkinController();
            RegisterEventListeners();
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();
        }

        private void FindSkinController()
        {
            // Tentar encontrar via DependencyManager primeiro
            if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
            {
                if (DependencyManager.Instance.TryGetForObject<SkinController>(_actor.ActorId, out var controller))
                {
                    _skinController = controller;
                }
            }

            // Fallback: buscar no mesmo GameObject
            if (_skinController == null)
            {
                _skinController = GetComponent<SkinController>();
            }

            // Fallback final: buscar em parents
            if (_skinController == null)
            {
                _skinController = GetComponentInParent<SkinController>();
            }

            if (_skinController != null)
            {
                // Registrar para eventos locais do SkinController
                _skinController.OnSkinInstancesCreated += OnLocalSkinInstancesCreated;
                _skinController.OnSkinApplied += OnLocalSkinApplied;
            }
        }

        private void InitializeDependencyRegistration()
        {
            if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
            {
                DependencyManager.Instance.RegisterForObject(_actor.ActorId, this);
            }
        }

        #region Event Registration
        private void RegisterEventListeners()
        {
            // Eventos globais filtrados por actor
            if (_actor != null)
            {
                _skinUpdateBinding = new EventBinding<SkinUpdateEvent>(OnGlobalSkinUpdate);
                _skinInstancesBinding = new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);
                
                FilteredEventBus<SkinUpdateEvent>.Register(_skinUpdateBinding, _actor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, _actor);
            }

            // Eventos globais não filtrados (se necessário para sistemas cross-actor)
            // EventBus<SkinUpdateEvent>.Register(new EventBinding<SkinUpdateEvent>(OnAnySkinUpdate));
        }

        private void UnregisterEventListeners()
        {
            if (_actor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.Unregister(_actor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(_actor);
            }

            if (_skinController != null)
            {
                _skinController.OnSkinInstancesCreated -= OnLocalSkinInstancesCreated;
                _skinController.OnSkinApplied -= OnLocalSkinApplied;
            }
        }
        #endregion

        #region Event Handlers
        private void OnGlobalSkinUpdate(SkinUpdateEvent evt)
        {
            if (evt.SkinConfig.ModelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }

        private void OnGlobalSkinInstancesCreated(SkinInstancesCreatedEvent evt)
        {
            if (evt.ModelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }

        private void OnLocalSkinApplied(ISkinConfig config)
        {
            if (config.ModelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }

        private void OnLocalSkinInstancesCreated(ModelType modelType, System.Collections.Generic.List<GameObject> instances)
        {
            if (modelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }
        #endregion

        private void RefreshAnimator()
        {
            _cachedAnimator = null;
            var newAnimator = ResolveAnimator();
            
            // Notificar via evento
            OnAnimatorChanged?.Invoke(newAnimator);
            
            // Notificar via DependencyManager
            if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
            {
                if (DependencyManager.Instance.TryGetForObject<AnimationControllerBase>(_actor.ActorId, out var controller))
                {
                    var localController = GetComponent<AnimationControllerBase>();
                    if (controller != null && controller != localController)
                    {
                        controller.OnAnimatorChanged(newAnimator);
                    }
                }
            }
        }

        private Animator ResolveAnimator()
        {
            // Tentar obter do SkinController primeiro
            if (_skinController != null)
            {
                var animators = _skinController.GetComponentsFromSkinInstances<Animator>(ModelType.ModelRoot);
                if (animators.Count > 0)
                {
                    return animators[0];
                }
            }

            // Fallback: buscar no GameObject atual
            return GetComponentInChildren<Animator>(true);
        }
    }
}