using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public interface IResourceLinkService
    {
        void RegisterLink(string actorId, ResourceLinkConfig linkConfig);
        void UnregisterLink(string actorId, ResourceType sourceResource);
        void UnregisterAllLinks(string actorId);
        bool HasLink(string actorId, ResourceType sourceResource);
        ResourceLinkConfig GetLink(string actorId, ResourceType sourceResource);
        float ProcessLinkedDrain(string actorId, ResourceType resourceType, float desiredDrain, ResourceSystem resourceSystem, ResourceChangeSource source = ResourceChangeSource.Manual);
    }

    [DebugLevel(DebugLevel.Warning)]
    public class ResourceLinkService : IResourceLinkService, IDisposable
    {
        private readonly Dictionary<string, Dictionary<ResourceType, ResourceLinkConfig>> _links = new();
        private EventBinding<ResourceUpdateEvent> _binding;
        private bool _disposed;

        public ResourceLinkService()
        {
            _binding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_binding);

            DependencyManager.Instance.RegisterGlobal<IResourceLinkService>(this);
            DebugUtility.LogVerbose<ResourceLinkService>("ResourceLinkService inicializado e registrado globalmente.");
        }

        public void RegisterLink(string actorId, ResourceLinkConfig cfg)
        {
            if (string.IsNullOrEmpty(actorId) || cfg == null) return;
            if (!_links.TryGetValue(actorId, out var dict))
            {
                dict = new Dictionary<ResourceType, ResourceLinkConfig>();
                _links[actorId] = dict;
            }
            dict[cfg.sourceResource] = cfg;
            DebugUtility.LogVerbose<ResourceLinkService>($"🔗 {actorId}: {cfg.sourceResource} -> {cfg.targetResource}");
        }

        public void UnregisterLink(string actorId, ResourceType src)
        {
            if (_links.TryGetValue(actorId, out var dict) && dict.Remove(src))
                DebugUtility.LogVerbose<ResourceLinkService>($"Link removido: {actorId} - {src}");
            if (dict != null && dict.Count == 0) _links.Remove(actorId);
        }

        public void UnregisterAllLinks(string actorId)
        {
            if (_links.Remove(actorId))
                DebugUtility.LogVerbose<ResourceLinkService>($"Todos os links removidos de {actorId}");
        }

        public bool HasLink(string actorId, ResourceType src) =>
            _links.TryGetValue(actorId, out var dict) && dict.ContainsKey(src);

        public ResourceLinkConfig GetLink(string actorId, ResourceType src) =>
            _links.TryGetValue(actorId, out var dict) && dict.TryGetValue(src, out var cfg) ? cfg : null;

        public float ProcessLinkedDrain(string actorId, ResourceType type, float desired, ResourceSystem sys, ResourceChangeSource source = ResourceChangeSource.Manual)
        {
            var cfg = GetLink(actorId, type);
            if (cfg == null || !cfg.affectTargetWithAutoFlow) return desired;

            var src = sys.Get(type);
            var tgt = sys.Get(cfg.targetResource);
            if (src == null || tgt == null) return desired;

            if (!cfg.ShouldTransfer(src.GetCurrentValue(), src.GetMaxValue())) return desired;

            float available = src.GetCurrentValue();
            float srcDrain = Mathf.Min(desired, available);
            float remaining = desired - srcDrain;
            if (remaining > 0) sys.Modify(cfg.targetResource, -remaining, source);

            DebugUtility.LogVerbose<ResourceLinkService>(
                $"AutoFlow link aplicado: {type}↓{srcDrain}, {cfg.targetResource}↓{remaining}");
            return srcDrain;
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (_disposed) return;
            var cfg = GetLink(evt.ActorId, evt.ResourceType);
            if (cfg == null) return;
            DebugUtility.LogVerbose<ResourceLinkService>(
                $"Evento de atualização em link ativo: {evt.ActorId} - {cfg.sourceResource} → {cfg.targetResource}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_binding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_binding);
                _binding = null;
            }

            _links.Clear();
            DebugUtility.LogVerbose<ResourceLinkService>("ResourceLinkService disposed.");
        }
    }
}
