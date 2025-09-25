using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.NewResourceSystem.UI
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceBinder : MonoBehaviour
    {
        private string actorId;
        private List<ICanvasResourceBinder> canvasBinders = new();
        private EntityResourceSystem resourceSystem;
        private WorldSpaceResourceBinder worldBinder;

        private void Start()
        {
            InitializeActorId();
            DiscoverResourceSystem();
            
            // 🔥 GARANTIR que o ResourceSystem está inicializado
            if (resourceSystem != null && !resourceSystem.IsInitialized)
            {
                resourceSystem.InitializeResources();
            }
            
            DiscoverWorldBinder();
            DiscoverCanvasBinders();
            BindAllResources();
            
            DebugUtility.LogVerbose<EntityResourceBinder>($"🎯 EntityBinder inicializado: {actorId}");
        }

        private void InitializeActorId()
        {
            var actor = GetComponent<IActor>();
            actorId = actor?.Name ?? gameObject.name;
            
            // 🔥 FORÇAR lowercase para consistência
            actorId = actorId.ToLower().Trim();
        }

        private void DiscoverResourceSystem()
        {
            resourceSystem = GetComponent<EntityResourceSystem>();
            if (resourceSystem == null)
            {
                DebugUtility.LogError<EntityResourceBinder>($"❌ EntityResourceSystem não encontrado em {actorId}");
            }
        }

        private void DiscoverWorldBinder()
        {
            worldBinder = GetComponentInChildren<WorldSpaceResourceBinder>();
            if (worldBinder != null)
            {
                worldBinder.Initialize(actorId, resourceSystem);
            }
        }

        private void DiscoverCanvasBinders()
        {
            // Buscar binders globais via DependencyManager
            if (DependencyManager.Instance.TryGetGlobal<ICanvasResourceBinder>(out var globalBinder))
            {
                canvasBinders.Add(globalBinder);
            }

            // Buscar binders específicos da cena
            string sceneName = gameObject.scene.name;
            if (DependencyManager.Instance.TryGetForScene<ICanvasResourceBinder>(sceneName, out var sceneBinder))
            {
                canvasBinders.Add(sceneBinder);
            }

            DebugUtility.LogVerbose<EntityResourceBinder>($"🔍 Encontrados {canvasBinders.Count} canvas binders para {actorId}");
        }

        private void BindAllResources()
        {
            if (resourceSystem == null) return;

            var resources = resourceSystem.GetAllResources();
            foreach (var resource in resources)
            {
                BindResource(resource.Key, resource.Value);
            }
        }

        public void BindResource(ResourceType type, IResourceValue data)
        {
            bool boundToAny = false;
            
            foreach (var binder in canvasBinders)
            {
                if (binder.TryBindActor(actorId, type, data))
                {
                    boundToAny = true;
                }
            }
            
            worldBinder?.BindResource(type, data);

            if (!boundToAny)
            {
                DebugUtility.LogWarning<EntityResourceBinder>($"⚠️ Nenhum canvas binder encontrado para {actorId}.{type}");
            }
            else
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"✅ Recurso vinculado: {actorId}.{type}");
            }
        }

        public void UnbindAll()
        {
            foreach (var binder in canvasBinders)
            {
                binder.UnbindActor(actorId);
            }
            
            worldBinder?.UnbindAll();
            
            DebugUtility.LogVerbose<EntityResourceBinder>($"🔓 Todos os recursos desvinculados: {actorId}");
        }

        private void OnDestroy()
        {
            UnbindAll();
            DebugUtility.LogVerbose<EntityResourceBinder>($"♻️ EntityBinder destruído: {actorId}");
        }

        [ContextMenu("Debug Binding")]
        public void DebugBinding()
        {
            DebugUtility.LogVerbose<EntityResourceBinder>($"🎯 Entity {actorId}:");
            DebugUtility.LogVerbose<EntityResourceBinder>($"   Canvas Binders: {canvasBinders.Count}");
            DebugUtility.LogVerbose<EntityResourceBinder>($"   World Binder: {(worldBinder != null ? "Sim" : "Não")}");
            
            if (resourceSystem != null)
            {
                DebugUtility.LogVerbose<EntityResourceBinder>($"   Recursos: {resourceSystem.GetAllResources().Count}");
            }
        }
    }
}