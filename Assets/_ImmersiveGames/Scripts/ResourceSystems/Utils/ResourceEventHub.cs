using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Utils
{
    /// <summary T=".">
    /// Adapter leve que integra a lógica de pendentes com o EventBus do projeto.
    /// Mantém pendentes por canvas (compatibilidade) e publica eventos via EventBus
    /// </summary>
    public static class ResourceEventHub
    {
// Pendentes organizados por canvasId -> (actorId, resourceType) -> request
        private static readonly Dictionary<string, Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest>> _pendingBindsByCanvas
            = new();

        // Public API (compatível): publica BindRequest no EventBus e registra pendente local caso ninguém execute
        public static void PublishBindRequest(CanvasBindRequest request)
        {
            // Publica no EventBus (todos os listeners irão tentar executar)
            EventBus<CanvasBindRequest>.Raise(request);

            // Registrar pendente local para caso ninguém processe (mantém compatibilidade)
            RegisterPendingBind(request);
        }

        public static void RegisterPendingBind(CanvasBindRequest request)
        {
            if (string.IsNullOrEmpty(request.targetCanvasId)) return;
            if (!_pendingBindsByCanvas.TryGetValue(request.targetCanvasId, out var canvasDict))
            {
                canvasDict = new Dictionary<(string, ResourceType), CanvasBindRequest>();
                _pendingBindsByCanvas[request.targetCanvasId] = canvasDict;
            }

            var key = (request.actorId, request.resourceType);
            canvasDict[key] = request;

            // NOVO: log para debug
            DebugUtility.LogVerbose(typeof(ResourceEventHub), $"🔖 Hub registered pending bind: {request.actorId}.{request.resourceType} -> {request.targetCanvasId}");
        }


        public static bool TryRemovePendingBind(string canvasId, string actorId, ResourceType resourceType)
        {
            if (!_pendingBindsByCanvas.TryGetValue(canvasId, out Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest> binds)) return false;
            var key = (actorId, resourceType);
            if (binds.Remove(key))
            {
                if (binds.Count == 0) _pendingBindsByCanvas.Remove(canvasId);
                return true;
            }
            return false;
        }

        public static IReadOnlyDictionary<(string actorId, ResourceType resourceType), CanvasBindRequest> GetPendingForCanvas(string canvasId)
        {
            if (_pendingBindsByCanvas.TryGetValue(canvasId, out Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest> binds)) return binds;
            return new Dictionary<(string, ResourceType), CanvasBindRequest>();
        }

        // Quando um canvas é registrado, reemitir todos os pendentes via EventBus para que listeners tentem executar
        public static void NotifyCanvasRegistered(string canvasId)
        {
            if (_pendingBindsByCanvas.TryGetValue(canvasId, out Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest> binds))
            {
                foreach (var req in binds.Values.ToList())
                {
                    EventBus<CanvasBindRequest>.Raise(req);
                }
            }

            // também publicar um evento de registro de canvas se quiser usar tipo forte:
            EventBus<CanvasRegisteredEvent>.Raise(new CanvasRegisteredEvent(canvasId));
        }

        public static void NotifyCanvasUnregistered(string canvasId)
        {
            _pendingBindsByCanvas.Remove(canvasId);
            EventBus<CanvasUnregisteredEvent>.Raise(new CanvasUnregisteredEvent(canvasId));
        }

        public static void NotifyActorRegistered(string actorId)
        {
            EventBus<ActorRegisteredEvent>.Raise(new ActorRegisteredEvent(actorId));
        }

        // Expõe types de evento simples para registro de canvas/actor
        public struct CanvasRegisteredEvent : IEvent
        {
            public readonly string canvasId;
            public CanvasRegisteredEvent(string id) { canvasId = id; }
        }
        private struct CanvasUnregisteredEvent : IEvent
        {
            public string canvasId;
            public CanvasUnregisteredEvent(string id) { canvasId = id; }
        }
        public struct ActorRegisteredEvent : IEvent
        {
            public readonly string actorId;
            public ActorRegisteredEvent(string id) { actorId = id; }
        }
        public struct CanvasBindRequest : IEvent
        {
            public readonly string key;
            public readonly string actorId;
            public readonly ResourceType resourceType;
            public readonly IResourceValue data;
            public readonly string targetCanvasId;
            public readonly float requestTime;

            public CanvasBindRequest(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
            {
                key = $"{actorId}_{resourceType}";
                this.actorId = actorId;
                this.resourceType = resourceType;
                this.data = data;
                this.targetCanvasId = targetCanvasId;
                requestTime = Time.time;
            }
        }
    }

}