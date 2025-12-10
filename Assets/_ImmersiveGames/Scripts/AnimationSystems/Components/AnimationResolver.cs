using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.SkinSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AnimationSystems.Components
{
    [DefaultExecutionOrder(-50)]
    public class AnimationResolver : MonoBehaviour, IAnimatorProvider
    {
        private Animator _cachedAnimator;
        private IActor _actor;
        private ActorSkinController _actorSkinController;

        private EventBinding<SkinEvents> _skinUpdateBinding;
        private EventBinding<SkinInstancesCreatedEvent> _skinInstancesBinding;

        private bool _listenersRegistered;

        public event System.Action<Animator> OnAnimatorChanged;

        public Animator GetAnimator() => _cachedAnimator ??= ResolveAnimator();

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            RegisterService();
        }

        private void Start()
        {
            FindSkinController();
            RegisterEventListeners();
        }

        private void OnDisable()
        {
            UnregisterEventListeners();
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();
        }

        private void RegisterService()
        {
            if (_actor == null || string.IsNullOrEmpty(_actor.ActorId))
            {
                return;
            }

            DependencyManager.Provider.RegisterForObject(_actor.ActorId, this as IAnimatorProvider);
            DebugUtility.LogVerbose<AnimationResolver>(
                $"IAnimatorProvider registrado para ActorId: {_actor.ActorId}",
                DebugUtility.Colors.Info);
        }

        private void FindSkinController()
        {
            if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
            {
                DependencyManager.Provider.TryGetForObject(_actor.ActorId, out _actorSkinController);
            }

            if (_actorSkinController == null)
            {
                _actorSkinController = GetComponent<ActorSkinController>() ?? GetComponentInParent<ActorSkinController>();
            }

            if (_actorSkinController != null)
            {
                _actorSkinController.OnSkinInstancesCreated += OnLocalActorSkinInstancesCreated;
                _actorSkinController.OnSkinApplied += OnLocalActorSkinApplied;
            }
        }

        private void RegisterEventListeners()
        {
            if (_actor == null)
            {
                return;
            }

            _skinUpdateBinding = new EventBinding<SkinEvents>(OnGlobalSkinUpdate);
            _skinInstancesBinding = new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);

            FilteredEventBus<SkinEvents>.Register(_skinUpdateBinding, _actor);
            FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, _actor);

            _listenersRegistered = true;
        }

        private void UnregisterEventListeners()
        {
            if (!_listenersRegistered)
            {
                return;
            }

            if (_actor != null)
            {
                if (_skinUpdateBinding != null)
                {
                    FilteredEventBus<SkinEvents>.Unregister(_skinUpdateBinding, _actor);
                }

                if (_skinInstancesBinding != null)
                {
                    FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(_skinInstancesBinding, _actor);
                }
            }

            if (_actorSkinController != null)
            {
                _actorSkinController.OnSkinInstancesCreated -= OnLocalActorSkinInstancesCreated;
                _actorSkinController.OnSkinApplied -= OnLocalActorSkinApplied;
            }

            _skinUpdateBinding = null;
            _skinInstancesBinding = null;
            _listenersRegistered = false;
        }

        private void OnGlobalSkinUpdate(SkinEvents evt)
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

        private void OnLocalActorSkinApplied(ISkinConfig config)
        {
            if (config.ModelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }

        private void OnLocalActorSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType == ModelType.ModelRoot)
            {
                RefreshAnimator();
            }
        }

        private void RefreshAnimator()
        {
            _cachedAnimator = null;
            var newAnimator = ResolveAnimator();
            OnAnimatorChanged?.Invoke(newAnimator);
        }

        private Animator ResolveAnimator()
        {
            if (_actorSkinController != null)
            {
                var animators = _actorSkinController.GetComponentsFromSkinInstances<Animator>(ModelType.ModelRoot);
                if (animators.Count > 0)
                {
                    return animators[0];
                }
            }

            return GetComponentInChildren<Animator>(true);
        }
    }
}
