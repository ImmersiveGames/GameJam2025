using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityUtils;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services
{
    /// <summary>
    /// Gerencia o registro e a comunicação entre canvases e o sistema de recursos.
    /// Funciona em modo event-driven via EventBus, sem polling e sem delays.
    /// </summary>
    public class RuntimeAttributeCanvasManager : PersistentSingleton<RuntimeAttributeCanvasManager>, IInjectableComponent
    {
        private readonly Dictionary<string, IAttributeCanvasBinder> _canvasRegistry = new();

        private EventBinding<RuntimeAttributeEventHub.CanvasBindRequest> _bindRequestBinding;
        private EventBinding<RuntimeAttributeEventHub.CanvasRegisteredEvent> _canvasRegisteredBinding;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => "RuntimeAttributeCanvasManager";

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            RuntimeAttributeBootstrapper.Instance.RegisterForInjection(this);
            DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>(
                "✅ Canvas Pipeline Manager initialized",
                DebugUtility.Colors.CrucialInfo);
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;

            // Escuta binds e eventos de registro de attributeCanvas
            _bindRequestBinding = new EventBinding<RuntimeAttributeEventHub.CanvasBindRequest>(OnBindRequestedHandler);
            EventBus<RuntimeAttributeEventHub.CanvasBindRequest>.Register(_bindRequestBinding);

            _canvasRegisteredBinding = new EventBinding<RuntimeAttributeEventHub.CanvasRegisteredEvent>(OnCanvasRegisteredHandler);
            EventBus<RuntimeAttributeEventHub.CanvasRegisteredEvent>.Register(_canvasRegisteredBinding);

            DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>(
                "✅ Dependencies injected and event bindings registered",
                DebugUtility.Colors.Success);
        }

        protected void OnDestroy()
        {
            if (_bindRequestBinding != null)
                EventBus<RuntimeAttributeEventHub.CanvasBindRequest>.Unregister(_bindRequestBinding);

            if (_canvasRegisteredBinding != null)
                EventBus<RuntimeAttributeEventHub.CanvasRegisteredEvent>.Unregister(_canvasRegisteredBinding);

            _canvasRegistry.Clear();
        }

        /// <summary>
        /// Registra um attributeCanvas no pipeline (idempotente).
        /// </summary>
        public void RegisterCanvas(IAttributeCanvasBinder attributeCanvas)
        {
            if (attributeCanvas == null || string.IsNullOrEmpty(attributeCanvas.CanvasId)) return;

            if (!_canvasRegistry.TryAdd(attributeCanvas.CanvasId, attributeCanvas))
            {
                DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>($"⚠️ Canvas '{attributeCanvas.CanvasId}' já está registrado — ignorado");
                return;
            }

            // Notificar o hub para reemitir binds pendentes
            RuntimeAttributeEventHub.NotifyCanvasRegistered(attributeCanvas.CanvasId);

            DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>(
                $"✅ Canvas '{attributeCanvas.CanvasId}' registrado no pipeline",
                DebugUtility.Colors.Success);
        }

        /// <summary>
        /// Remove um attributeCanvas do pipeline e notifica o hub.
        /// </summary>
        public void UnregisterCanvas(string canvasId)
        {
            if (string.IsNullOrEmpty(canvasId)) return;

            if (_canvasRegistry.Remove(canvasId))
            {
                RuntimeAttributeEventHub.NotifyCanvasUnregistered(canvasId);
                DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>(
                    $"Canvas '{canvasId}' removido do pipeline",
                    DebugUtility.Colors.Success);
            }
        }

        /// <summary>
        /// Solicita um bind. Se o attributeCanvas estiver pronto, executa imediatamente; caso contrário, delega ao hub/eventbus.
        /// </summary>
        public void ScheduleBind(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data, string targetCanvasId)
        {
            if (string.IsNullOrEmpty(targetCanvasId) || string.IsNullOrEmpty(actorId)) return;

            var request = new RuntimeAttributeEventHub.CanvasBindRequest(actorId, runtimeAttributeType, data, targetCanvasId);

            if (TryExecuteBind(request))
            {
                DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>($"✅ Bind imediato: {actorId}.{runtimeAttributeType} → {targetCanvasId}");
                return;
            }

            // Se o attributeCanvas não estiver pronto, delega via hub/eventbus
            RuntimeAttributeEventHub.PublishBindRequest(request);
            DebugUtility.LogVerbose<RuntimeAttributeCanvasManager>($"📦 Bind delegado: {actorId}.{runtimeAttributeType} → {targetCanvasId}");
        }

        /// <summary>
        /// Tenta executar um bind diretamente se o attributeCanvas estiver disponível e pronto.
        /// </summary>
        private bool TryExecuteBind(RuntimeAttributeEventHub.CanvasBindRequest request)
        {
            if (_canvasRegistry.TryGetValue(request.targetCanvasId, out var canvas) && canvas.CanAcceptBinds())
            {
                canvas.ScheduleBind(request.actorId, request.runtimeAttributeType, request.data);
                RuntimeAttributeEventHub.TryRemovePendingBind(request.targetCanvasId, request.actorId, request.runtimeAttributeType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evento: tentativa de bind publicada via EventBus.
        /// </summary>
        private void OnBindRequestedHandler(RuntimeAttributeEventHub.CanvasBindRequest request)
        {
            TryExecuteBind(request);
        }

        /// <summary>
        /// Evento: reprocessar binds pendentes quando um attributeCanvas é registrado.
        /// </summary>
        private void OnCanvasRegisteredHandler(RuntimeAttributeEventHub.CanvasRegisteredEvent evt)
        {
            if (!_canvasRegistry.TryGetValue(evt.canvasId, out _)) return;

            IReadOnlyDictionary<(string actorId, RuntimeAttributeType resourceType), RuntimeAttributeEventHub.CanvasBindRequest> pendingRequests = RuntimeAttributeEventHub.GetPendingForCanvas(evt.canvasId);
            foreach (var req in pendingRequests.Values.ToList())
                TryExecuteBind(req);
        }
    }
}
