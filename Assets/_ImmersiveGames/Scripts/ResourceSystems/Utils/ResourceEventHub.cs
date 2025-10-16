using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Utils
{
    /// <summary>
    /// Adaptador entre o EventBus e o gerenciamento de binds pendentes.
    /// Mantém compatibilidade entre pipelines desacopladas.
    /// </summary>
    public static class ResourceEventHub
    {
        private static readonly Dictionary<string, Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest>> _pendingBinds = new();

        // --- PUBLICAÇÃO DE EVENTOS ------------------------------------------------------------

        public static void PublishBindRequest(CanvasBindRequest request)
        {
            EventBus<CanvasBindRequest>.Raise(request);
            RegisterPendingBind(request);
        }

        public static void RegisterPendingBind(CanvasBindRequest request)
        {
            if (string.IsNullOrEmpty(request.targetCanvasId)) return;

            var dict = _pendingBinds.GetValueOrDefault(request.targetCanvasId)
                       ?? (_pendingBinds[request.targetCanvasId] = new());

            dict[(request.actorId, request.resourceType)] = request;

            DebugUtility.LogVerbose(typeof(ResourceEventHub),
                $"📦 Pending bind registered: {request.actorId}.{request.resourceType} -> {request.targetCanvasId}");
        }

        public static bool TryRemovePendingBind(string canvasId, string actorId, ResourceType resourceType)
        {
            if (!_pendingBinds.TryGetValue(canvasId, out var dict)) return false;
            if (!dict.Remove((actorId, resourceType))) return false;

            if (dict.Count == 0) _pendingBinds.Remove(canvasId);
            return true;
        }

        public static IReadOnlyDictionary<(string actorId, ResourceType resourceType), CanvasBindRequest> GetPendingForCanvas(string canvasId)
        {
            return _pendingBinds.TryGetValue(canvasId, out var dict)
                ? dict
                : new Dictionary<(string, ResourceType), CanvasBindRequest>();
        }

        // --- NOTIFICAÇÕES SIMPLES ------------------------------------------------------------

        public static void NotifyCanvasRegistered(string canvasId)
        {
            if (_pendingBinds.TryGetValue(canvasId, out var binds))
            {
                foreach (var req in binds.Values.ToList())
                    EventBus<CanvasBindRequest>.Raise(req);
            }

            EventBus<CanvasRegisteredEvent>.Raise(new CanvasRegisteredEvent(canvasId));
        }

        public static void NotifyCanvasUnregistered(string canvasId)
        {
            _pendingBinds.Remove(canvasId);
            EventBus<CanvasUnregisteredEvent>.Raise(new CanvasUnregisteredEvent(canvasId));
        }

        public static void NotifyActorRegistered(string actorId)
        {
            EventBus<ActorRegisteredEvent>.Raise(new ActorRegisteredEvent(actorId));
        }

        // --- EVENTOS ------------------------------------------------------------

        public struct CanvasRegisteredEvent : IEvent { public readonly string canvasId; public CanvasRegisteredEvent(string id) => canvasId = id; }
        private struct CanvasUnregisteredEvent : IEvent { public readonly string canvasId; public CanvasUnregisteredEvent(string id) => canvasId = id; }
        public struct ActorRegisteredEvent : IEvent { public readonly string actorId; public ActorRegisteredEvent(string id) => actorId = id; }

        public struct CanvasBindRequest : IEvent
        {
            public readonly string actorId;
            public readonly ResourceType resourceType;
            public readonly IResourceValue data;
            public readonly string targetCanvasId;
            public readonly float requestTime;

            public CanvasBindRequest(string actorId, ResourceType type, IResourceValue data, string targetCanvasId)
            {
                this.actorId = actorId;
                this.resourceType = type;
                this.data = data;
                this.targetCanvasId = targetCanvasId;
                this.requestTime = Time.time;
            }
        }
    }
}
