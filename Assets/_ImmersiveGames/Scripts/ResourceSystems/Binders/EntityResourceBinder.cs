using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            // Removido warning, pois algumas entidades (ex: player01) não têm WorldSpaceResourceBinder
        }

        private void OnBinderRegistered(CanvasBinderRegisteredEvent evt)
        {
            if (!_canvasBinders.Contains(evt.Binder))
            {
                _canvasBinders.Add(evt.Binder);
                DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 CanvasBinder adicionado dinamicamente: {evt.Binder.CanvasId}");
                BindAllResources();
            }
        }

        private void DiscoverCanvasBinders()
        {
            _canvasBinders.Clear();
            if (DependencyManager.Instance != null)
            {
                // Busca binders em todas as cenas carregadas
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    var binders = new List<ICanvasResourceBinder>();
                    if (DependencyManager.Instance.TryGetForScene<ICanvasResourceBinder>(scene.name, out var binder))
                    {
                        binders.Add(binder);
                        DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 CanvasBinder encontrado na cena {scene.name}: {binder.CanvasId}");
                    }
                    _canvasBinders.AddRange(binders);
                }
                DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 Total de CanvasBinders encontrados: {_canvasBinders.Count}");
                if (_canvasBinders.Count == 0)
                {
                    DebugUtility.LogWarning<EntityResourceBinder>($"⚠️ Nenhum CanvasResourceBinder encontrado nas cenas carregadas.");
                }
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
            
            foreach (var binder in _canvasBinders)
            {
                if (binder.TryBindActor(_actorId, type, data))
                {
                    boundToAny = true;
                    DebugUtility.LogVerbose<EntityResourceBinder>($"✅ Recurso vinculado: {_actorId}.{type} em {binder.CanvasId}");
                }
            }
            
            _worldBinder?.BindResource(type, data);

            if (!boundToAny && _worldBinder == null)
            {
                DebugUtility.LogWarning<EntityResourceBinder>($"⚠️ Nenhum slot encontrado para {_actorId}.{type} em nenhuma cena ou WorldSpaceResourceBinder.");
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
            foreach (var binder in _canvasBinders)
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"     - {binder.CanvasId}");
            }
            DebugUtility.LogVerbose<EntityResourceBinder>($"   World Binder: {(_worldBinder != null ? "Sim" : "Não")}");
            
            if (_resourceSystem != null)
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"   Recursos: {_resourceSystem.GetAllResources().Count}");
            }
        }
    }
}