using System;
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

            // Obter o serviço de links
            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Instance.RegisterGlobal(_linkService);
            }

            // Registrar todos os links configurados
            foreach (var linkConfig in resourceLinks)
            {
                if (linkConfig != null)
                {
                    _linkService.RegisterLink(Actor.ActorId, linkConfig);
                    DebugUtility.LogVerbose<ResourceLinkBridge>($"Registered link: {linkConfig.sourceResource} -> {linkConfig.targetResource}");
                }
            }

            OnServiceInitialized();
            return true;
        }

        protected override void OnServiceInitialized()
        {
            DebugUtility.LogVerbose<ResourceLinkBridge>($"ResourceLinkBridge inicializado com {resourceLinks.Length} links");
        }

        protected override void OnServiceDispose()
        {
            if (_linkService == null || Actor == null) return;
            _linkService.UnregisterAllLinks(Actor.ActorId);
            DebugUtility.LogVerbose<ResourceLinkBridge>("Todos os links removidos");
        }

        protected override void OnInitializationFailed()
        {
            DebugUtility.LogWarning<ResourceLinkBridge>("Falha na inicialização do ResourceLinkBridge");
        }

        [ContextMenu("Debug Active Links")]
        public void DebugActiveLinks()
        {
            if (_linkService == null || Actor == null) 
            {
                DebugUtility.LogWarning<ResourceLinkBridge>("Serviço de links ou ator não disponível");
                return;
            }

            DebugUtility.LogVerbose<ResourceLinkBridge>($"Active resource links for {Actor.ActorId}:");
            foreach (var linkConfig in resourceLinks)
            {
                if (linkConfig == null) continue;
                bool isActive = _linkService.HasLink(Actor.ActorId, linkConfig.sourceResource);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"  {linkConfig.sourceResource} -> {linkConfig.targetResource}: {(isActive ? "ACTIVE" : "INACTIVE")}");
            }
        }

        [ContextMenu("Force Re-register Links")]
        public void ForceReregisterLinks()
        {
            if (_linkService == null || Actor == null) 
            {
                DebugUtility.LogWarning<ResourceLinkBridge>("Serviço de links ou ator não disponível");
                return;
            }

            // Remover todos os links primeiro
            _linkService.UnregisterAllLinks(Actor.ActorId);

            // Registrar novamente
            foreach (var linkConfig in resourceLinks)
            {
                if (linkConfig != null)
                {
                    _linkService.RegisterLink(Actor.ActorId, linkConfig);
                }
            }

            DebugUtility.LogVerbose<ResourceLinkBridge>("Links re-registrados com sucesso");
        }

        // Métodos públicos para adicionar/remover links em tempo de execução
        public void AddLink(ResourceLinkConfig linkConfig)
        {
            if (_linkService != null && Actor != null && linkConfig != null)
            {
                _linkService.RegisterLink(Actor.ActorId, linkConfig);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"Link adicionado: {linkConfig.sourceResource} -> {linkConfig.targetResource}");
            }
        }

        public void RemoveLink(ResourceType sourceResource)
        {
            if (_linkService != null && Actor != null)
            {
                _linkService.UnregisterLink(Actor.ActorId, sourceResource);
                DebugUtility.LogVerbose<ResourceLinkBridge>($"Link removido: {sourceResource}");
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

        // Método para verificar se um recurso tem links ativos
        public bool HasAnyLinks()
        {
            return _linkService != null && Actor != null && resourceLinks.Length > 0;
        }

        // Método para obter todos os links configurados
        public ResourceLinkConfig[] GetAllLinks()
        {
            return resourceLinks;
        }
        public new void DebugStatus()
        {
            DebugUtility.LogWarning<ResourceLinkBridge>($"Link Bridge Status - Actor: {Actor?.ActorId}, Links: {resourceLinks.Length}, Service: {_linkService != null}");
        }
    }
}