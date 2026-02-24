using System;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges
{
    
    public class RuntimeAttributeLinkBridge : RuntimeAttributeBridgeBase
    {
        [Header("Resource Links")]
        [SerializeField] private RuntimeAttributeLinkConfig[] resourceLinks = Array.Empty<RuntimeAttributeLinkConfig>();

        private IRuntimeAttributeLinkService _linkService;

        protected override void OnServiceInitialized()
        {
            if (resourceLinks.Length == 0)
            {
                DebugUtility.LogVerbose<RuntimeAttributeLinkBridge>($"Nenhum link configurado em {actor.ActorId}. Component desativado.");
                enabled = false;
                return;
            }

            // Obtem ou cria o serviço global
            if (!DependencyManager.Provider.TryGetGlobal(out _linkService))
            {
                _linkService = new RuntimeAttributeLinkService();
                DependencyManager.Provider.RegisterGlobal(_linkService);
            }

            foreach (var link in resourceLinks)
            {
                if (link == null)
                {
                    DebugUtility.LogWarning<RuntimeAttributeLinkBridge>("Configuração de link nula ignorada.");
                    continue;
                }

                _linkService.RegisterLink(actor.ActorId, link);
                DebugUtility.LogVerbose<RuntimeAttributeLinkBridge>($"?? {link.sourceRuntimeAttribute} ? {link.targetRuntimeAttribute} registrado");
            }

            DebugUtility.LogVerbose<RuntimeAttributeLinkBridge>(
                $"? RuntimeAttributeLinkBridge ativo com {resourceLinks.Length} links para {actor.ActorId}",
                DebugUtility.Colors.Success);
        }

        protected override void OnServiceDispose()
        {
            if (_linkService == null || actor == null) return;
            _linkService.UnregisterAllLinks(actor.ActorId);
            DebugUtility.LogVerbose<RuntimeAttributeLinkBridge>(
                "??? Todos os links removidos",
                DebugUtility.Colors.Success);
        }

        public void AddLink(RuntimeAttributeLinkConfig link)
        {
            if (_linkService == null || actor == null || link == null) return;
            _linkService.RegisterLink(actor.ActorId, link);
            DebugUtility.LogVerbose<RuntimeAttributeLinkBridge>($"? Link adicionado: {link.sourceRuntimeAttribute} -> {link.targetRuntimeAttribute}");
        }

        public bool HasLink(RuntimeAttributeType src) =>
            _linkService != null && actor != null && _linkService.HasLink(actor.ActorId, src);

        public RuntimeAttributeLinkConfig[] GetAllLinks() => resourceLinks;
    }
}

