using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Gerencia o registro e a comunicação entre canvases e o sistema de recursos.
    /// Opera em modo event-driven via EventBus, sem polling e sem delays.
    /// </summary>
    public class CanvasPipelineManager : PersistentSingleton<CanvasPipelineManager>, IInjectableComponent
    {
        private readonly Dictionary<string, ICanvasBinder> _canvasRegistry = new();

        private EventBinding<ResourceEventHub.CanvasBindRequest> _bindRequestBinding;
        private EventBinding<ResourceEventHub.CanvasRegisteredEvent> _canvasRegisteredBinding;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => "CanvasPipelineManager";

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            ResourceInitializationManager.Instance.RegisterForInjection(this);
            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Canvas Pipeline Manager initialized");
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;

            // Escuta binds e eventos de registro de canvas
            _bindRequestBinding = new EventBinding<ResourceEventHub.CanvasBindRequest>(OnBindRequestedHandler);
            EventBus<ResourceEventHub.CanvasBindRequest>.Register(_bindRequestBinding);

            _canvasRegisteredBinding = new EventBinding<ResourceEventHub.CanvasRegisteredEvent>(OnCanvasRegisteredHandler);
            EventBus<ResourceEventHub.CanvasRegisteredEvent>.Register(_canvasRegisteredBinding);

            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Dependencies injected and event bindings registered");
        }

        protected void OnDestroy()
        {
            if (_bindRequestBinding != null)
                EventBus<ResourceEventHub.CanvasBindRequest>.Unregister(_bindRequestBinding);

            if (_canvasRegisteredBinding != null)
                EventBus<ResourceEventHub.CanvasRegisteredEvent>.Unregister(_canvasRegisteredBinding);

            _canvasRegistry.Clear();
        }

        /// <summary>
        /// Registra um canvas no pipeline (idempotente).
        /// </summary>
        public void RegisterCanvas(ICanvasBinder canvas)
        {
            if (canvas == null || string.IsNullOrEmpty(canvas.CanvasId)) return;

            if (!_canvasRegistry.TryAdd(canvas.CanvasId, canvas))
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"⚠️ Canvas '{canvas.CanvasId}' já está registrado — ignorado");
                return;
            }

            // Notificar o hub para reemitir binds pendentes
            ResourceEventHub.NotifyCanvasRegistered(canvas.CanvasId);

            DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Canvas '{canvas.CanvasId}' registrado no pipeline");
        }

        /// <summary>
        /// Remove um canvas do pipeline e notifica o hub.
        /// </summary>
        public void UnregisterCanvas(string canvasId)
        {
            if (string.IsNullOrEmpty(canvasId)) return;

            if (_canvasRegistry.Remove(canvasId))
            {
                ResourceEventHub.NotifyCanvasUnregistered(canvasId);
                DebugUtility.LogVerbose<CanvasPipelineManager>($"Canvas '{canvasId}' removido do pipeline");
            }
        }

        /// <summary>
        /// Solicita um bind. Se o canvas estiver pronto, executa imediatamente; caso contrário, delega ao hub/eventbus.
        /// </summary>
        public void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            if (string.IsNullOrEmpty(targetCanvasId) || string.IsNullOrEmpty(actorId)) return;

            var request = new ResourceEventHub.CanvasBindRequest(actorId, resourceType, data, targetCanvasId);

            if (TryExecuteBind(request))
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Bind imediato: {actorId}.{resourceType} → {targetCanvasId}");
                return;
            }

            // Se o canvas não estiver pronto, delega via hub/eventbus
            ResourceEventHub.PublishBindRequest(request);
            DebugUtility.LogVerbose<CanvasPipelineManager>($"📦 Bind delegado: {actorId}.{resourceType} → {targetCanvasId}");
        }

        /// <summary>
        /// Tenta executar um bind diretamente se o canvas estiver disponível e pronto.
        /// </summary>
        private bool TryExecuteBind(ResourceEventHub.CanvasBindRequest request)
        {
            if (_canvasRegistry.TryGetValue(request.targetCanvasId, out var canvas) && canvas.CanAcceptBinds())
            {
                canvas.ScheduleBind(request.actorId, request.resourceType, request.data);
                ResourceEventHub.TryRemovePendingBind(request.targetCanvasId, request.actorId, request.resourceType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evento: tentativa de bind publicada via EventBus.
        /// </summary>
        private void OnBindRequestedHandler(ResourceEventHub.CanvasBindRequest request)
        {
            TryExecuteBind(request);
        }

        /// <summary>
        /// Evento: reprocessar binds pendentes quando um canvas é registrado.
        /// </summary>
        private void OnCanvasRegisteredHandler(ResourceEventHub.CanvasRegisteredEvent evt)
        {
            if (!_canvasRegistry.TryGetValue(evt.canvasId, out var canvas)) return;

            var pendings = ResourceEventHub.GetPendingForCanvas(evt.canvasId);
            foreach (var req in pendings.Values.ToList())
                TryExecuteBind(req);
        }
    }
}
