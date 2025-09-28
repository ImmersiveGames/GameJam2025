using System;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    [DefaultExecutionOrder(-5)]
    public class EntityResourceBridge : MonoBehaviour
    {
        [SerializeField] private string entityId;
        [SerializeField] private ResourceInstanceConfig[] resourceInstances = Array.Empty<ResourceInstanceConfig>();

        private ResourceSystemService _service;
        private IActorResourceOrchestrator _orchestrator;

        private void Awake()
        {
            if (string.IsNullOrEmpty(entityId))
                entityId = gameObject.name;

            _service = new ResourceSystemService(entityId, resourceInstances);

            DependencyManager.Instance.RegisterForObject(entityId, _service);

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                var orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);
                _orchestrator = orchestrator;
            }

            _orchestrator.RegisterActor(_service);
        }

        private void OnDestroy()
        {
            if (_orchestrator != null)
                _orchestrator.UnregisterActor(entityId);

            DependencyManager.Instance.ClearObjectServices(entityId);
            _service?.Dispose();
            _service = null;
        }

        [ContextMenu("Debug Print Resources")]
        private void DebugPrintResources()
        {
            var all = _service.GetAll();
            Debug.Log($"[EntityResourceBridge] {entityId} resources count: {all.Count}");
            foreach (var kv in all)
                Debug.Log($"  - {kv.Key}: {kv.Value.GetCurrentValue()}/{kv.Value.GetMaxValue()}");
        }

        public ResourceSystemService GetService() => _service;
    }
}
