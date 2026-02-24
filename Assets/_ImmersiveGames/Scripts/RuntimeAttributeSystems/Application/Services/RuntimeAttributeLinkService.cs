using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services
{
    public interface IRuntimeAttributeLinkService
    {
        void RegisterLink(string actorId, RuntimeAttributeLinkConfig linkConfig);
        void UnregisterLink(string actorId, RuntimeAttributeType sourceRuntimeAttribute);
        void UnregisterAllLinks(string actorId);
        bool HasLink(string actorId, RuntimeAttributeType sourceRuntimeAttribute);
        RuntimeAttributeLinkConfig GetLink(string actorId, RuntimeAttributeType sourceRuntimeAttribute);
        float ProcessLinkedDrain(string actorId, RuntimeAttributeType runtimeAttributeType, float desiredDrain, RuntimeAttributeContext runtimeAttributeContext, RuntimeAttributeChangeSource source = RuntimeAttributeChangeSource.Manual);
    }

    
    public class RuntimeAttributeLinkService : IRuntimeAttributeLinkService, IDisposable
    {
        private readonly Dictionary<string, Dictionary<RuntimeAttributeType, RuntimeAttributeLinkConfig>> _links = new();
        private EventBinding<RuntimeAttributeUpdateEvent> _binding;
        private bool _disposed;

        public RuntimeAttributeLinkService()
        {
            _binding = new EventBinding<RuntimeAttributeUpdateEvent>(OnResourceUpdated);
            EventBus<RuntimeAttributeUpdateEvent>.Register(_binding);

            DependencyManager.Provider.RegisterGlobal<IRuntimeAttributeLinkService>(this);
            DebugUtility.LogVerbose<RuntimeAttributeLinkService>("RuntimeAttributeLinkService inicializado e registrado globalmente.");
        }

        public void RegisterLink(string actorId, RuntimeAttributeLinkConfig cfg)
        {
            if (string.IsNullOrEmpty(actorId) || cfg == null) return;
            if (!_links.TryGetValue(actorId, out var dict))
            {
                dict = new Dictionary<RuntimeAttributeType, RuntimeAttributeLinkConfig>();
                _links[actorId] = dict;
            }
            dict[cfg.sourceRuntimeAttribute] = cfg;
            DebugUtility.LogVerbose<RuntimeAttributeLinkService>($"🔗 {actorId}: {cfg.sourceRuntimeAttribute} -> {cfg.targetRuntimeAttribute}");
        }

        public void UnregisterLink(string actorId, RuntimeAttributeType src)
        {
            if (_links.TryGetValue(actorId, out var dict) && dict.Remove(src))
                DebugUtility.LogVerbose<RuntimeAttributeLinkService>($"Link removido: {actorId} - {src}");
            if (dict is { Count: 0 }) _links.Remove(actorId);
        }

        public void UnregisterAllLinks(string actorId)
        {
            if (_links.Remove(actorId))
                DebugUtility.LogVerbose<RuntimeAttributeLinkService>($"Todos os links removidos de {actorId}");
        }

        public bool HasLink(string actorId, RuntimeAttributeType src) =>
            _links.TryGetValue(actorId, out var dict) && dict.ContainsKey(src);

        public RuntimeAttributeLinkConfig GetLink(string actorId, RuntimeAttributeType src) =>
            _links.TryGetValue(actorId, out var dict) && dict.TryGetValue(src, out var cfg) ? cfg : null;

        public float ProcessLinkedDrain(string actorId, RuntimeAttributeType type, float desired, RuntimeAttributeContext sys, RuntimeAttributeChangeSource source = RuntimeAttributeChangeSource.Manual)
        {
            var cfg = GetLink(actorId, type);
            if (cfg == null || !cfg.affectTargetWithAutoFlow) return desired;

            var src = sys.Get(type);
            var tgt = sys.Get(cfg.targetRuntimeAttribute);
            if (src == null || tgt == null) return desired;

            if (!cfg.ShouldTransfer(src.GetCurrentValue(), src.GetMaxValue())) return desired;

            float available = src.GetCurrentValue();
            float srcDrain = Mathf.Min(desired, available);
            float remaining = desired - srcDrain;
            if (remaining > 0) sys.Modify(cfg.targetRuntimeAttribute, -remaining, source);

            DebugUtility.LogVerbose<RuntimeAttributeLinkService>(
                $"AutoFlow link aplicado: {type}↓{srcDrain}, {cfg.targetRuntimeAttribute}↓{remaining}");
            return srcDrain;
        }

        private void OnResourceUpdated(RuntimeAttributeUpdateEvent evt)
        {
            if (_disposed) return;
            var cfg = GetLink(evt.ActorId, evt.RuntimeAttributeType);
            if (cfg == null) return;
            DebugUtility.LogVerbose<RuntimeAttributeLinkService>(
                $"Evento de atualização em link ativo: {evt.ActorId} - {cfg.sourceRuntimeAttribute} → {cfg.targetRuntimeAttribute}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_binding != null)
            {
                EventBus<RuntimeAttributeUpdateEvent>.Unregister(_binding);
                _binding = null;
            }

            _links.Clear();
            DebugUtility.LogVerbose<RuntimeAttributeLinkService>("RuntimeAttributeLinkService disposed.");
        }
    }
}

