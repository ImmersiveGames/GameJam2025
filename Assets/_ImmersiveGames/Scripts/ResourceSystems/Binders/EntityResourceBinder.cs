using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceBinder : MonoBehaviour
    {
        private string _actorId;
        private readonly List<ICanvasResourceBinder> _canvasBinders = new();
        private EntityResourceSystem _resourceSystem;
        private WorldSpaceResourceBinder _worldBinder;

        private void Start()
        {
            InitializeActorId();
            DiscoverResourceSystem();
            
            // 🔥 GARANTIR que o ResourceSystem está inicializado
            if (_resourceSystem != null && !_resourceSystem.IsInitialized)
            {
                _resourceSystem.InitializeResources();
            }
            
            DiscoverWorldBinder();
            DiscoverCanvasBinders();
            BindAllResources();
            
            DebugUtility.LogVerbose<EntityResourceBinder>($"🎯 EntityBinder inicializado: {_actorId}");
        }

        private void InitializeActorId()
        {
            var actor = GetComponent<IActor>();
            _actorId = actor?.Name ?? gameObject.name;
            
            // 🔥 FORÇAR lowercase para consistência
            _actorId = _actorId.ToLower().Trim();
        }

        private void DiscoverResourceSystem()
        {
            _resourceSystem = GetComponent<EntityResourceSystem>();
            if (_resourceSystem == null)
            {
                DebugUtility.LogError<EntityResourceBinder>($"❌ EntityResourceSystem não encontrado em {_actorId}");
            }
        }

        private void DiscoverWorldBinder()
        {
            _worldBinder = GetComponentInChildren<WorldSpaceResourceBinder>();
            if (_worldBinder != null)
            {
                _worldBinder.Initialize(_actorId, _resourceSystem);
            }
        }

        private void DiscoverCanvasBinders()
        {
            // Buscar binders globais via DependencyManager
            if (DependencyManager.Instance.TryGetGlobal<ICanvasResourceBinder>(out var globalBinder))
            {
                _canvasBinders.Add(globalBinder);
            }

            // Buscar binders específicos da cena
            string sceneName = gameObject.scene.name;
            if (DependencyManager.Instance.TryGetForScene<ICanvasResourceBinder>(sceneName, out var sceneBinder))
            {
                _canvasBinders.Add(sceneBinder);
            }

            DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 Encontrados {_canvasBinders.Count} canvas binders para {_actorId}");
        }

        private void BindAllResources()
        {
            if (_resourceSystem == null) return;

            Dictionary<ResourceType, IResourceValue> resources = _resourceSystem.GetAllResources();
            foreach (KeyValuePair<ResourceType, IResourceValue> resource in resources)
            {
                BindResource(resource.Key, resource.Value);
            }
        }

        public void BindResource(ResourceType type, IResourceValue data)
        {
            bool boundToAny = false;
            
            foreach (var binder in _canvasBinders.Where(binder => binder.TryBindActor(_actorId, type, data)))
            {
                boundToAny = true;
            }
            
            _worldBinder?.BindResource(type, data);

            if (!boundToAny)
            {
                DebugUtility.LogWarning<EntityResourceBinder>($"⚠️ Nenhum canvas binder encontrado para {_actorId}.{type}");
            }
            else
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"✅ Recurso vinculado: {_actorId}.{type}");
            }
        }

        public void UnbindAll()
        {
            foreach (var binder in _canvasBinders)
            {
                binder.UnbindActor(_actorId);
            }
            
            _worldBinder?.UnbindAll();
            
            DebugUtility.LogVerbose<EntityResourceBinder>($"🔓 Todos os recursos desvinculados: {_actorId}");
        }

        private void OnDestroy()
        {
            UnbindAll();
            DebugUtility.LogVerbose<EntityResourceBinder>($"♻️ EntityBinder destruído: {_actorId}");
        }

        [ContextMenu("Debug Binding")]
        public void DebugBinding()
        {
            DebugUtility.LogVerbose<EntityResourceBinder>($"🎯 Entity {_actorId}:");
            DebugUtility.LogVerbose<EntityResourceBinder>($"   Canvas Binders: {_canvasBinders.Count}");
            DebugUtility.LogVerbose<EntityResourceBinder>($"   World Binder: {(_worldBinder != null ? "Sim" : "Não")}");
            
            if (_resourceSystem != null)
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"   Recursos: {_resourceSystem.GetAllResources().Count}");
            }
        }
    }
}