using System;
using System.Collections.Generic;
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
                DebugUtility.LogVerbose<ResourceLinkBridge>("Nenhum link configurado. Desativando.");
                enabled = false;
                return true;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Instance.RegisterGlobal(_linkService);
                DebugUtility.LogVerbose<ResourceLinkBridge>("Criado novo ResourceLinkService global");
            }

            int registeredCount = 0;
            foreach (var linkConfig in resourceLinks)
            {
                if (linkConfig != null)
                {
                    _linkService.RegisterLink(Actor.ActorId, linkConfig);
                    registeredCount++;
                    DebugUtility.LogVerbose<ResourceLinkBridge>($"✅ Registered link: {linkConfig.sourceResource} -> {linkConfig.targetResource}");
                }
                else
                {
                    DebugUtility.LogWarning<ResourceLinkBridge>($"⚠️ Link config é null");
                }
            }

            DebugUtility.LogVerbose<ResourceLinkBridge>($"📋 Total de links registrados: {registeredCount}/{resourceLinks.Length}");
            return registeredCount > 0;
        }

        protected override void OnServiceInitialized()
        {
            DebugUtility.LogVerbose<ResourceLinkBridge>($"🔗 ResourceLinkBridge inicializado com {resourceLinks.Length} links para {Actor.ActorId}");
        }

        protected override void OnServiceDispose()
        {
            if (_linkService == null || Actor == null) return;

            _linkService.UnregisterAllLinks(Actor.ActorId);
            DebugUtility.LogVerbose<ResourceLinkBridge>("🗑️ Todos os links removidos");
        }

        protected override void OnInitializationFailed()
        {
            DebugUtility.LogWarning<ResourceLinkBridge>($"❌ Falha na inicialização do ResourceLinkBridge para {Actor?.ActorId}");
        }

        protected override void Update()
        {
            base.Update();

            // Sincronização com bind: Apenas processar se canvas pronto (opcional, pois o link é passivo via eventos)
            if (initialized && _orchestrator.IsCanvasRegisteredForActor(Actor.ActorId))
            {
                // Lógica passiva - já é gerenciada por eventos no ResourceLinkService
            }
        }

        public void AddLink(ResourceLinkConfig linkConfig)
        {
            if (_linkService != null && Actor != null && linkConfig != null)
            {
                _linkService.RegisterLink(Actor.ActorId, linkConfig);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"➕ Link adicionado: {linkConfig.sourceResource} -> {linkConfig.targetResource}");
            }
        }

        public void RemoveLink(ResourceType sourceResource)
        {
            if (_linkService != null && Actor != null)
            {
                _linkService.UnregisterLink(Actor.ActorId, sourceResource);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"➖ Link removido: {sourceResource}");
            }
        }

        public bool HasLink(ResourceType sourceResource)
        {
            return _linkService != null && Actor != null && _linkService.HasLink(Actor.ActorId, sourceResource);
        }

        public ResourceLinkConfig GetLink(ResourceType sourceResource)
        {
            return _linkService?.GetLink(Actor.ActorId, sourceResource);
        }

        public bool HasAnyLinks()
        {
            return _linkService != null && Actor != null && resourceLinks.Length > 0;
        }

        public ResourceLinkConfig[] GetAllLinks()
        {
            return resourceLinks;
        }
    }
}