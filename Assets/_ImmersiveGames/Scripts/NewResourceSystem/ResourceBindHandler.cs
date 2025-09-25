using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.NewResourceSystem
{
    public class ResourceBindHandler : BaseBindHandler<ResourceBindEvent, MonoBehaviour, IResourceValue>,
                                      IActorRegistry, IResourceUpdater
    {
        
        [Inject] private IUIFactory<ResourceBindEvent, IResourceUI> _uiFactory;
        
        // ✅ MULTIPLAYER - Registro de atores
        private readonly HashSet<string> _registeredActors = new();

        protected void Awake()
        {
            // ✅ REGISTRAR NO DI SYSTEM (SEM FindObjectOfType)
            if (DependencyManager.Instance != null)
            {
                DependencyManager.Instance.RegisterGlobal<IActorRegistry>(this);
                DependencyManager.Instance.RegisterGlobal<IResourceUpdater>(this);
            }
        }

        public override void HandleEvent(ResourceBindEvent evt)
        {
            if (_uiFactory == null || string.IsNullOrEmpty(evt.ActorId)) 
            {
                DebugUtility.LogError<ResourceBindHandler>("❌ Factory not injected or invalid ActorId");
                return;
            }

            // ✅ CHAVE COMPOSTA para multi-recursos
            string bindingKey = CreateBindingKey(evt.ActorId, evt.Type);
            
            if (bindings.ContainsKey(bindingKey)) 
            {
                DebugUtility.LogVerbose<ResourceBindHandler>($"⚠️ Binding already exists: {bindingKey}");
                return;
            }

            var uiInstance = CreateUI(evt);
            if (uiInstance != null)
            {
                bindings.Add(bindingKey, uiInstance);
                
                // ✅ REGISTRAR ACTOR para multiplayer
                RegisterActor(evt.ActorId);
                
                // ✅ VINCULAR UI
                if (uiInstance is IBindableUI<IResourceValue> bindableUI)
                    bindableUI.Bind(evt.ActorId, evt.Resource);
                
                if (uiInstance is IResourceUI resourceUI)
                    resourceUI.SetResourceType(evt.Type);

                DebugUtility.LogVerbose<ResourceBindHandler>($"✅ UI Created for {bindingKey}");
            }
        }

        protected override MonoBehaviour CreateUI(ResourceBindEvent evt)
        {
            var ui = _uiFactory?.CreateUI(evt, transform);
            return ui as MonoBehaviour;
        }

        // ✅ IResourceUpdater IMPLEMENTATION
        public void UpdateResource(string actorId, ResourceType resourceType, IResourceValue newValue)
        {
            UpdateBinding(actorId, resourceType, newValue);
        }

        public void UpdateActorResources(string actorId, Dictionary<ResourceType, IResourceValue> resources)
        {
            foreach (var resource in resources)
            {
                UpdateResource(actorId, resource.Key, resource.Value);
            }
        }

        public IResourceUI GetResourceUI(string actorId, ResourceType resourceType)
        {
            string key = CreateBindingKey(actorId, resourceType);
            if (bindings.TryGetValue(key, out var ui) && ui is IResourceUI resourceUI)
                return resourceUI;
            return null;
        }

        public List<IResourceUI> GetActorUIs(string actorId)
        {
            var result = new List<IResourceUI>();
            foreach (var kvp in bindings)
            {
                if (kvp.Key.StartsWith(actorId + "_") && kvp.Value is IResourceUI resourceUI)
                    result.Add(resourceUI);
            }
            return result;
        }

        public void SetActorUIVisible(string actorId, bool visible)
        {
            var uis = GetActorUIs(actorId);
            foreach (var ui in uis)
            {
                ui.SetVisible(visible);
            }
        }

        public bool HasBinding(string actorId, ResourceType resourceType)
        {
            string key = CreateBindingKey(actorId, resourceType);
            return bindings.ContainsKey(key);
        }

        // ✅ IActorRegistry IMPLEMENTATION
        public void RegisterActor(string actorId)
        {
            if (_registeredActors.Add(actorId))
            {
                DebugUtility.LogVerbose<ResourceBindHandler>($"🎮 Actor registered: {actorId}");
            }
        }

        public void UnregisterActor(string actorId)
        {
            if (_registeredActors.Remove(actorId))
            {
                RemoveActorBindings(actorId);
                DebugUtility.LogVerbose<ResourceBindHandler>($"🎮 Actor unregistered: {actorId}");
            }
        }

        public bool IsActorRegistered(string actorId) => _registeredActors.Contains(actorId);

        public void RemoveActorBindings(string actorId)
        {
            var keysToRemove = bindings.Keys.Where(key => key.StartsWith(actorId + "_")).ToList();
            foreach (var key in keysToRemove)
            {
                if (bindings[key] is IResourceUI resourceUI)
                    resourceUI.Unbind();
                bindings.Remove(key);
            }
        }

        public int GetResourceCountForActor(string actorId)
        {
            return bindings.Keys.Count(key => key.StartsWith(actorId + "_"));
        }

        // ✅ DEBUG METHODS
        [ContextMenu("Debug Registered Actors")]
        public void DebugRegisteredActors()
        {
            DebugUtility.LogVerbose<ResourceBindHandler>($"🎮 Registered Actors: {_registeredActors.Count}");
            foreach (var actorId in _registeredActors)
            {
                var resourceCount = GetResourceCountForActor(actorId);
                DebugUtility.LogVerbose<ResourceBindHandler>($"   {actorId}: {resourceCount} resources");
            }
        }
    }
}