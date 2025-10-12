using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public interface IResourceLinkService
    {
        void RegisterLink(string actorId, ResourceLinkConfig linkConfig);
        void UnregisterLink(string actorId, ResourceType sourceResource);
        void UnregisterAllLinks(string actorId);
        bool HasLink(string actorId, ResourceType sourceResource);
        ResourceLinkConfig GetLink(string actorId, ResourceType sourceResource);
    }
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceLinkService : IResourceLinkService, IDisposable
    {
        private readonly Dictionary<string, Dictionary<ResourceType, ResourceLinkConfig>> _actorLinks = new();
        private readonly IActorResourceOrchestrator _orchestrator;
        private bool _isDisposed;
        private EventBinding<ResourceUpdateEvent> _eventBinding;

        public ResourceLinkService()
        {
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                _orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal(_orchestrator);
            }

            // Registrar para eventos de modificação de recursos
            _eventBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_eventBinding);

            // Registrar como serviço global
            DependencyManager.Instance.RegisterGlobal<IResourceLinkService>(this);

            DebugUtility.LogVerbose<ResourceLinkService>("ResourceLinkService inicializado");
        }

        public void RegisterLink(string actorId, ResourceLinkConfig linkConfig)
        {
            if (string.IsNullOrEmpty(actorId) || linkConfig == null) return;

            if (!_actorLinks.TryGetValue(actorId, out Dictionary<ResourceType, ResourceLinkConfig> links))
            {
                links = new Dictionary<ResourceType, ResourceLinkConfig>();
                _actorLinks[actorId] = links;
            }

            links[linkConfig.sourceResource] = linkConfig;
            DebugUtility.LogVerbose<ResourceLinkService>($"Link registrado: {actorId} - {linkConfig.sourceResource} -> {linkConfig.targetResource}");
        }

        public void UnregisterLink(string actorId, ResourceType sourceResource)
        {
            if (_actorLinks.TryGetValue(actorId, out Dictionary<ResourceType, ResourceLinkConfig> links))
            {
                if (links.Remove(sourceResource))
                {
                    DebugUtility.LogVerbose<ResourceLinkService>($"Link removido: {actorId} - {sourceResource}");
                }
                
                if (links.Count == 0)
                {
                    _actorLinks.Remove(actorId);
                }
            }
        }

        public void UnregisterAllLinks(string actorId)
        {
            if (_actorLinks.Remove(actorId))
            {
                DebugUtility.LogVerbose<ResourceLinkService>($"Todos os links removidos para: {actorId}");
            }
        }

        public bool HasLink(string actorId, ResourceType sourceResource)
        {
            return _actorLinks.TryGetValue(actorId, out Dictionary<ResourceType, ResourceLinkConfig> links) && links.ContainsKey(sourceResource);
        }

        public ResourceLinkConfig GetLink(string actorId, ResourceType sourceResource)
        {
            if (_actorLinks.TryGetValue(actorId, out Dictionary<ResourceType, ResourceLinkConfig> links) && links.TryGetValue(sourceResource, out var linkConfig))
            {
                return linkConfig;
            }
            return null;
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (_isDisposed) return;

            // Verificar se há links para este recurso
            var linkConfig = GetLink(evt.ActorId, evt.ResourceType);
            if (linkConfig == null) return;

            // Obter o ResourceSystem do ator
            var resourceSystem = _orchestrator.GetActorResourceSystem(evt.ActorId);
            if (resourceSystem == null) return;

            // Obter valores atuais dos recursos
            var sourceResource = resourceSystem.Get(linkConfig.sourceResource);
            var targetResource = resourceSystem.Get(linkConfig.targetResource);

            if (sourceResource == null || targetResource == null) return;

            // Verificar condições de transferência
            if (linkConfig.ShouldTransfer(sourceResource.GetCurrentValue(), sourceResource.GetMaxValue()))
            {
                DebugUtility.LogVerbose<ResourceLinkService>(
                    $"Link ativado: {evt.ActorId} - {linkConfig.sourceResource} -> {linkConfig.targetResource}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            
            if (_eventBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_eventBinding);
                _eventBinding = null;
            }
            
            _actorLinks.Clear();
            
            DebugUtility.LogVerbose<ResourceLinkService>("ResourceLinkService disposed");
        }
    }
}