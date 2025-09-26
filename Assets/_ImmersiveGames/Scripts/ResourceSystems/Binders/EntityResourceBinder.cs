// EntityResourceBinder.cs
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
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
            _actorId = actor?.ActorName ?? gameObject.name;
            _actorId = _actorId.ToLower().Trim();
        }

        private void DiscoverResourceSystem()
        {
            _resourceSystem = GetComponent<EntityResourceSystem>();
            EventBus<CanvasBinderRegisteredEvent>.Register(new EventBinding<CanvasBinderRegisteredEvent>(OnBinderRegistered));
            if (_resourceSystem == null)
            {
                DebugUtility.LogError<EntityResourceBinder>($"❌ EntityResourceSystem não encontrado em {_actorId}");
            }
        }
        private void DiscoverWorldBinder()
        {
            _worldBinder = GetComponentInChildren<WorldSpaceResourceBinder>(true);
            if (_worldBinder != null)
            {
                _worldBinder.Initialize(_actorId, _resourceSystem);
                DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 WorldBinder encontrado para {_actorId}");
            }
            else
            {
                DebugUtility.LogWarning<EntityResourceBinder>($"⚠️ WorldSpaceResourceBinder não encontrado para {_actorId}");
            }
        }

        private void OnBinderRegistered(CanvasBinderRegisteredEvent evt)
        {
            if (!_canvasBinders.Contains(evt.Binder))
            {
                _canvasBinders.Add(evt.Binder);
                DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 CanvasBinder adicionado: {evt.Binder.CanvasId}");
                BindAllResources();
            }
        }

        private void DiscoverCanvasBinders()
        {
            _canvasBinders.Clear();
            if (DependencyManager.Instance != null)
            {
                // Busca binders na cena "UI" (ou outras cenas relevantes)
                string[] uiScenes = { "UI" }; // Ajuste se houver outras cenas UI
                foreach (var sceneName in uiScenes)
                {
                    var binders = new List<ICanvasResourceBinder>();
                    if (DependencyManager.Instance.TryGetForScene<ICanvasResourceBinder>(sceneName, out var binder))
                    {
                        binders.Add(binder);
                        DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 CanvasBinder encontrado na cena {sceneName}: {binder.CanvasId}");
                    }
                    _canvasBinders.AddRange(binders);
                }
                DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 Total de CanvasBinders encontrados: {_canvasBinders.Count}");
            }
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
            EventBus<CanvasBinderRegisteredEvent>.Unregister(new EventBinding<CanvasBinderRegisteredEvent>(OnBinderRegistered));
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