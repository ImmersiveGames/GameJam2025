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
        public ResourceSystem GetResourceSystem() => _service;
        public bool IsDestroyed() => _isDestroyed;

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

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Awake called for {_actor.ActorId}");
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                if (DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem existingService))
                {
                    Debug.LogWarning($"[EntityBridge] ResourceSystem already exists for {_actor.ActorId}, reusing");
                    _service = existingService;
                }
                else
                {
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"✅ Created new ResourceSystem for '{_actor.ActorId}'");
                }

                if (_orchestrator != null && !_orchestrator.IsActorRegistered(_actor.ActorId))
                {
                    _orchestrator.RegisterActor(_service);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"✅ Registered actor '{_actor.ActorId}' in orchestrator");
                }
                else
                {
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Actor '{_actor.ActorId}' already registered in orchestrator");
                }

                InjectionState = DependencyInjectionState.Ready;

                DebugResourceSystemAccess();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>($"❌ Entity bridge failed for '{_actor.ActorId}': {ex}");
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void DebugResourceSystemAccess()
        {
            StartCoroutine(DebugResourceSystemCoroutine());
        }

        private System.Collections.IEnumerator DebugResourceSystemCoroutine()
        {
            yield return new WaitForSeconds(1f);

            Debug.Log($"[EntityBridge] 🔍 POST-INIT CHECK for {_actor.ActorId}:");
            Debug.Log($"  - Local _service: {_service != null}");
            Debug.Log($"  - InjectionState: {InjectionState}");

            bool hasInDm = DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem dmService);
            Debug.Log($"  - In DependencyManager: {hasInDm}, Service: {dmService != null}");

            if (_orchestrator != null)
            {
                bool hasInOrchestrator = _orchestrator.IsActorRegistered(_actor.ActorId);
                Debug.Log($"  - In Orchestrator: {hasInOrchestrator}");

                if (hasInOrchestrator)
                {
                    var orchestratorService = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    Debug.Log($"  - Orchestrator Service: {orchestratorService != null}");
                }
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"OnDestroy called for {_actor?.ActorId}");

            if (_orchestrator != null && _actor != null)
            {
                _orchestrator.UnregisterActor(_actor.ActorId);
                DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Unregistered actor '{_actor.ActorId}' from orchestrator");
            }

            if (_actor != null)
            {
                DependencyManager.Instance.ClearObjectServices(_actor.ActorId);
                DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Cleared services for '{_actor.ActorId}' from DependencyManager");
            }

            _service?.Dispose();
            _service = null;
        }
    }
}