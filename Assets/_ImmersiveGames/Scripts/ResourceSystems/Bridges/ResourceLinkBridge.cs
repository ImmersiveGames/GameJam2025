using System;
using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceLinkBridge : ResourceBridgeBase
    {
        [Header("Resource Links")]
        [SerializeField] private ResourceLinkConfig[] resourceLinks = Array.Empty<ResourceLinkConfig>();

        private IResourceLinkService _linkService;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            if (resourceLinks.Length == 0)
            {
                DebugUtility.LogVerbose<ResourceLinkBridge>(
                    $"Nenhum link configurado para {Actor.ActorId}. Bridge desativado (sem erro).");
                enabled = false;
                return true;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Instance.RegisterGlobal(_linkService);
                DebugUtility.LogVerbose<ResourceLinkBridge>("Criado novo ResourceLinkService global");
            }

            foreach (var linkConfig in resourceLinks)
            {
                if (linkConfig == null)
                {
                    DebugUtility.LogWarning<ResourceLinkBridge>("⚠️ Link config é null");
                    continue;
                }

                _linkService.RegisterLink(Actor.ActorId, linkConfig);
                DebugUtility.LogVerbose<ResourceLinkBridge>(
                    $"✅ Registered link: {linkConfig.sourceResource} -> {linkConfig.targetResource}");
            }

            DebugUtility.LogVerbose<ResourceLinkBridge>(
                $"🔗 ResourceLinkBridge configurado com {resourceLinks.Length} links para {Actor.ActorId}");
            return true;
        }

        protected override void OnServiceInitialized()
        {
            DebugUtility.LogVerbose<ResourceLinkBridge>(
                $"🚀 ResourceLinkBridge inicializado para {Actor.ActorId}");
        }
        public void AddLink(ResourceLinkConfig linkConfig) { if (_linkService != null && Actor != null && linkConfig != null) { _linkService.RegisterLink(Actor.ActorId, linkConfig); DebugUtility.LogVerbose<ResourceLinkBridge>($"➕ Link adicionado: {linkConfig.sourceResource} -> {linkConfig.targetResource}"); } }
        protected override void OnServiceDispose()
        {
            if (_linkService == null || Actor == null) return;
            _linkService.UnregisterAllLinks(Actor.ActorId);
            DebugUtility.LogVerbose<ResourceLinkBridge>("🗑️ Todos os links removidos");
        }
        public bool HasLink(ResourceType sourceResource) { return _linkService != null && Actor != null && _linkService.HasLink(Actor.ActorId, sourceResource); }
        public ResourceLinkConfig[] GetAllLinks() { return resourceLinks; }
    }
}
