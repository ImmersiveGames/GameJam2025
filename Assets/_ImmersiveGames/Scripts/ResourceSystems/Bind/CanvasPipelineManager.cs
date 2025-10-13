using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class CanvasPipelineManager : PersistentSingleton<CanvasPipelineManager>, IInjectableComponent
    {
        private readonly Dictionary<string, ICanvasBinder> _canvasRegistry = new();

        // bindings registrados no EventBus (para poder desregistrar se necessário)
        private EventBinding<ResourceEventHub.CanvasBindRequest> _bindRequestBinding;
        private EventBinding<ResourceEventHub.CanvasRegisteredEvent> _canvasRegisteredBinding;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => "CanvasPipelineManager";

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Pipeline Manager fully initialized with dependencies");

            // registrar listeners no EventBus
            _bindRequestBinding = new EventBinding<ResourceEventHub.CanvasBindRequest>(OnBindRequestedHandler);
            EventBus<ResourceEventHub.CanvasBindRequest>.Register(_bindRequestBinding);

            _canvasRegisteredBinding = new EventBinding<ResourceEventHub.CanvasRegisteredEvent>(OnCanvasRegisteredHandler);
            EventBus<ResourceEventHub.CanvasRegisteredEvent>.Register(_canvasRegisteredBinding);
        }

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            ResourceInitializationManager.Instance.RegisterForInjection(this);
            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Canvas Pipeline Manager Ready");
        }

        protected void OnDestroy()
        {
            try
            {
                if (_bindRequestBinding != null) EventBus<ResourceEventHub.CanvasBindRequest>.Unregister(_bindRequestBinding);
                if (_canvasRegisteredBinding != null) EventBus<ResourceEventHub.CanvasRegisteredEvent>.Unregister(_canvasRegisteredBinding);
            }
            catch
            {
                // ignored
            }
        }

        public void RegisterCanvas(ICanvasBinder canvas)
        {
            if (canvas == null) return;

            if (!_canvasRegistry.TryAdd(canvas.CanvasId, canvas))
            {
                DebugUtility.LogWarning<CanvasPipelineManager>($"Canvas '{canvas.CanvasId}' already registered in pipeline");
                return;
            }

            // notificamos o adapter/hub para reemitir pendentes
            ResourceEventHub.NotifyCanvasRegistered(canvas.CanvasId);

            DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Canvas '{canvas.CanvasId}' registered in pipeline");
        }

        public void UnregisterCanvas(string canvasId)
        {
            if (_canvasRegistry.Remove(canvasId))
            {
                ResourceEventHub.NotifyCanvasUnregistered(canvasId);
                DebugUtility.LogVerbose<CanvasPipelineManager>($"Canvas '{canvasId}' unregistered from pipeline");
            }
        }

        // Compat layer: recebe pedidos de bind e tenta executar imediatamente; caso falhe, o hub (adapter) guarda pendentes
        public void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            var request = new ResourceEventHub.CanvasBindRequest(actorId, resourceType, data, targetCanvasId);

            if (TryExecuteBind(request))
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Immediate bind: {actorId}.{resourceType} -> {targetCanvasId}");
                return;
            }

            // publica no hub que delega pro EventBus e guarda pendentes
            ResourceEventHub.PublishBindRequest(request);
            DebugUtility.LogVerbose<CanvasPipelineManager>($"📦 Delegated bind to hub: {actorId}.{resourceType} -> {targetCanvasId}");
        }

        private bool TryExecuteBind(ResourceEventHub.CanvasBindRequest request)
        {
            if (_canvasRegistry.TryGetValue(request.targetCanvasId, out var canvas) && canvas.CanAcceptBinds())
            {
                canvas.ScheduleBind(request.actorId, request.resourceType, request.data);

                // se havia pendente, remover do adapter
                ResourceEventHub.TryRemovePendingBind(request.targetCanvasId, request.actorId, request.resourceType);
                return true;
            }
            return false;
        }

        // Handler chamado via EventBus quando um bind é publicado
        private void OnBindRequestedHandler(ResourceEventHub.CanvasBindRequest request) => TryExecuteBind(request);

        // Handler para reprocessar pendentes ao registrar canvas (também chamado via ResourceEventHub.NotifyCanvasRegistered)
        private void OnCanvasRegisteredHandler(ResourceEventHub.CanvasRegisteredEvent evt)
        {
            string canvasId = evt.canvasId;
            if (!_canvasRegistry.TryGetValue(canvasId, out var canvas)) return;

            IReadOnlyDictionary<(string actorId, ResourceType resourceType), ResourceEventHub.CanvasBindRequest> pendings = ResourceEventHub.GetPendingForCanvas(canvasId);
            foreach (var req in pendings.Values.ToList())
            {
                TryExecuteBind(req);
            }
        }

        [ContextMenu("🔍 DEBUG PIPELINE")]
        public void DebugPipeline()
        {
            Debug.Log($"🎯 PIPELINE MANAGER DEBUG:");
            Debug.Log($"- Registered Canvases: {_canvasRegistry.Count}");
            foreach (string canvasId in _canvasRegistry.Keys)
            {
                var canvas = _canvasRegistry[canvasId];
                Debug.Log($"  - {canvasId} (State: {canvas.State}, AcceptsBinds: {canvas.CanAcceptBinds()})");
            }
            Debug.Log($"- Pending binds: use ResourceEventHub.GetPendingForCanvas(canvasId) para detalhes");
        }
    }

}