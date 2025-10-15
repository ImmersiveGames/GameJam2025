using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    [DefaultExecutionOrder(25)]
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
                DebugUtility.LogWarning<InjectableEntityResourceBridge>(
                    $"No IActor found on {gameObject.name}. Bridge disabled.");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                $"Awake called for {_actor.ActorId}");
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                // 🔹 Verifica se já existe um ResourceSystem para este actor
                if (DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem existingService) && existingService != null)
                {
                    _service = existingService;
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                        $"♻️ Reusing existing ResourceSystem for '{_actor.ActorId}'");
                }
                else
                {
                    // 🔹 Cria um novo se necessário
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                        $"✅ Created new ResourceSystem for '{_actor.ActorId}'");
                }

                // 🔹 Registro no orchestrator se ainda não existir
                if (_orchestrator != null)
                {
                    if (!_orchestrator.IsActorRegistered(_actor.ActorId))
                    {
                        _orchestrator.RegisterActor(_service);
                        DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                            $"✅ Registered actor '{_actor.ActorId}' in orchestrator");
                    }
                    else
                    {
                        DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                            $"Actor '{_actor.ActorId}' already registered in orchestrator");
                    }
                }
                else
                {
                    DebugUtility.LogWarning<InjectableEntityResourceBridge>(
                        $"Orchestrator not found during bridge init for '{_actor.ActorId}'");
                }

                InjectionState = DependencyInjectionState.Ready;

                // ✅ Emite status de debug após injeção
                LogPostInitializationStatus();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>(
                    $"❌ Entity bridge failed for '{_actor.ActorId}': {ex}");
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void LogPostInitializationStatus()
        {
            DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"[EntityBridge] 🔍 POST-INIT CHECK for {_actor.ActorId}:");

            bool hasDM = DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out var dmSvc);
            bool inOrchestrator = _orchestrator != null && _orchestrator.IsActorRegistered(_actor.ActorId);

            Debug.Log(
                $"  - Local _service: {_service != null}\n" +
                $"  - InjectionState: {InjectionState}\n" +
                $"  - In DependencyManager: {hasDM}, Service: {dmSvc != null}\n" +
                $"  - In Orchestrator: {inOrchestrator}");
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                $"OnDestroy called for {_actor?.ActorId}");

            try
            {
                if (_actor != null && _orchestrator != null)
                {
                    _orchestrator.UnregisterActor(_actor.ActorId);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                        $"Unregistered actor '{_actor.ActorId}' from orchestrator");
                }

                if (_actor != null)
                {
                    DependencyManager.Instance.ClearObjectServices(_actor.ActorId);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>(
                        $"Cleared services for '{_actor.ActorId}' from DependencyManager");
                }

                _service?.Dispose();
                _service = null;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>(
                    $"Error on destroy for '{_actor?.ActorId}': {ex}");
            }
        }
    }
}
