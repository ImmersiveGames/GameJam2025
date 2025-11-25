using System;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    
    public class ResourceLinkBridge : ResourceBridgeBase
    {
        [Header("Resource Links")]
        [SerializeField] private ResourceLinkConfig[] resourceLinks = Array.Empty<ResourceLinkConfig>();

        private IResourceLinkService _linkService;

        protected override void OnServiceInitialized()
        {
            if (resourceLinks.Length == 0)
            {
                DebugUtility.LogVerbose<ResourceLinkBridge>($"Nenhum link configurado em {actor.ActorId}. Bridge desativado.");
                enabled = false;
                return;
            }

            // Obtem ou cria o serviço global
            if (!DependencyManager.Provider.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Provider.RegisterGlobal(_linkService);
            }

            foreach (var link in resourceLinks)
            {
                if (link == null)
                {
                    DebugUtility.LogWarning<ResourceLinkBridge>("Configuração de link nula ignorada.");
                    continue;
                }

                _linkService.RegisterLink(actor.ActorId, link);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"🔗 {link.sourceResource} → {link.targetResource} registrado");
            }

            DebugUtility.LogVerbose<ResourceLinkBridge>(
                $"✅ ResourceLinkBridge ativo com {resourceLinks.Length} links para {actor.ActorId}",
                DebugUtility.Colors.Success);
        }

        protected override void OnServiceDispose()
        {
            if (_linkService == null || actor == null) return;
            _linkService.UnregisterAllLinks(actor.ActorId);
            DebugUtility.LogVerbose<ResourceLinkBridge>(
                "🗑️ Todos os links removidos",
                DebugUtility.Colors.Success);
        }

        public void AddLink(ResourceLinkConfig link)
        {
            if (_linkService == null || actor == null || link == null) return;
            _linkService.RegisterLink(actor.ActorId, link);
            DebugUtility.LogVerbose<ResourceLinkBridge>($"➕ Link adicionado: {link.sourceResource} -> {link.targetResource}");
        }

        public bool HasLink(ResourceType src) =>
            _linkService != null && actor != null && _linkService.HasLink(actor.ActorId, src);

        public ResourceLinkConfig[] GetAllLinks() => resourceLinks;
    }
}
