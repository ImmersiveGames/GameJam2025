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
        private SkinController _skinController;

        private EventBinding<SkinUpdateEvent> _skinUpdateBinding;
        private EventBinding<SkinInstancesCreatedEvent> _skinInstancesBinding;

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

        private void RegisterService()
        {
            if (_actor == null || string.IsNullOrEmpty(_actor.ActorId)) return;

            DependencyManager.Provider.RegisterForObject(_actor.ActorId, this as IAnimatorProvider);
            DebugUtility.LogVerbose<AnimationResolver>($"IAnimatorProvider registrado para {_actor.ActorId}");
        }

        private void FindSkinController()
        {
            if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
            {
                DependencyManager.Provider.TryGetForObject(_actor.ActorId, out _skinController);
            }

            if (_skinController == null)
                _skinController = GetComponent<SkinController>() ?? GetComponentInParent<SkinController>();

            if (_skinController != null)
            {
                _skinController.OnSkinInstancesCreated += OnLocalSkinInstancesCreated;
                _skinController.OnSkinApplied += OnLocalSkinApplied;
            }
        }

        private void RegisterEventListeners()
        {
            if (_actor == null) return;

            _skinUpdateBinding = new EventBinding<SkinUpdateEvent>(OnGlobalSkinUpdate);
            _skinInstancesBinding = new EventBinding<SkinInstancesCreatedEvent>(OnGlobalSkinInstancesCreated);

            FilteredEventBus<SkinUpdateEvent>.Register(_skinUpdateBinding, _actor);
            FilteredEventBus<SkinInstancesCreatedEvent>.Register(_skinInstancesBinding, _actor);
        }

        private void UnregisterEventListeners()
        {
            if (_actor != null)
            {
                FilteredEventBus<SkinUpdateEvent>.Unregister(_skinUpdateBinding, _actor);
                FilteredEventBus<SkinInstancesCreatedEvent>.Unregister(_skinInstancesBinding, _actor);
            }

            if (_skinController != null)
            {
                _skinController.OnSkinInstancesCreated -= OnLocalSkinInstancesCreated;
                _skinController.OnSkinApplied -= OnLocalSkinApplied;
            }
        }

        private void OnGlobalSkinUpdate(SkinUpdateEvent evt)
        {
            if (evt.SkinConfig.ModelType == ModelType.ModelRoot) RefreshAnimator();
        }

        private void OnGlobalSkinInstancesCreated(SkinInstancesCreatedEvent evt)
        {
            if (evt.ModelType == ModelType.ModelRoot) RefreshAnimator();
        }

        private void OnLocalSkinApplied(ISkinConfig config)
        {
            if (config.ModelType == ModelType.ModelRoot) RefreshAnimator();
        }

        private void OnLocalSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType == ModelType.ModelRoot) RefreshAnimator();
        }

        private void RefreshAnimator()
        {
            _cachedAnimator = null;
            var newAnimator = ResolveAnimator();
            OnAnimatorChanged?.Invoke(newAnimator);
        }

        private Animator ResolveAnimator()
        {
            if (_skinController != null)
            {
                var animators = _skinController.GetComponentsFromSkinInstances<Animator>(ModelType.ModelRoot);
                if (animators.Count > 0) return animators[0];
            }

            return GetComponentInChildren<Animator>(true);
        }
    }
}