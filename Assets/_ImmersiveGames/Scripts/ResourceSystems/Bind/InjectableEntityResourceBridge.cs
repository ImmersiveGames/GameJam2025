using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class InjectableEntityResourceBridge : MonoBehaviour, IInjectableComponent
    {
        [SerializeField] private ResourceInstanceConfig[] resourceInstances = Array.Empty<ResourceInstanceConfig>();

        [Inject] private IActorResourceOrchestrator _orchestrator;

        private IActor _actor;
        private ResourceSystem _service;
        private bool _isDestroyed;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => _actor?.ActorId ?? gameObject.name;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<InjectableEntityResourceBridge>($"No IActor found on {gameObject.name}");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                if (!DependencyManager.Instance.TryGetForObject(_actor.ActorId, out _service))
                {
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);
                }

                if (_orchestrator != null && !_orchestrator.IsActorRegistered(_actor.ActorId))
                    _orchestrator.RegisterActor(_service);

                InjectionState = DependencyInjectionState.Ready;
                DebugUtility.Log<InjectableEntityResourceBridge>(
                    $"✅ Bridge initialized for '{_actor.ActorId}'",
                    DebugUtility.Colors.CrucialInfo);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>($"❌ Entity bridge failed for '{_actor.ActorId}': {ex}");
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            try
            {
                if (_actor != null)
                {
                    _orchestrator?.UnregisterActor(_actor.ActorId);
                    DependencyManager.Instance.ClearObjectServices(_actor.ActorId);
                }

                _service?.Dispose();
                _service = null;

                DebugUtility.Log<InjectableEntityResourceBridge>(
                    $"Cleaned up bridge for '{_actor?.ActorId}'",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>($"Error on destroy: {ex}");
            }
        }
        public ResourceSystem GetResourceSystem() => _service;
    }
}
