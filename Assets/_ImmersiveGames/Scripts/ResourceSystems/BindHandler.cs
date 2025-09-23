using System.Collections.Generic;
using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class BindHandler : MonoBehaviour
    {
        [SerializeField] private GameObject resourceUIPrefab;
        private readonly Dictionary<string, ResourceUI> _uiBindings = new Dictionary<string, ResourceUI>();
        private EventBinding<ResourceBindEvent> _bindEventBinding;

        private void Awake()
        {
            if (resourceUIPrefab == null || resourceUIPrefab.GetComponent<ResourceUI>() == null)
            {
                DebugUtility.LogError<BindHandler>("resourceUIPrefab not assigned or does not contain ResourceUI!", this);
                return;
            }
        }

        private void OnEnable()
        {
            _bindEventBinding = new EventBinding<ResourceBindEvent>(OnResourceBind);
            EventBus<ResourceBindEvent>.Register(_bindEventBinding);
            DebugUtility.LogVerbose<BindHandler>($"OnEnable: Registered for ResourceBindEvent, Source={gameObject.name}");
        }

        private void OnDisable()
        {
            if (_bindEventBinding != null)
                EventBus<ResourceBindEvent>.Unregister(_bindEventBinding);
            DebugUtility.LogVerbose<BindHandler>($"OnDisable: Unregistered from ResourceBindEvent, Source={gameObject.name}");
        }

        private void OnResourceBind(ResourceBindEvent evt)
        {
            if (_uiBindings.ContainsKey(evt.ActorId))
            {
                DebugUtility.LogVerbose<BindHandler>($"OnResourceBind: ActorId={evt.ActorId} already bound, Source={evt.Source.name}");
                return;
            }

            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            var uiInstance = Instantiate(resourceUIPrefab, transform).GetComponent<ResourceUI>();
            uiInstance.SetActorId(evt.ActorId);
            uiInstance.SetResourceId(resourceId);
            uiInstance.SetResourceType(evt.Type);
            uiInstance.SetBindHandler(this);
            uiInstance.SetResource(evt.Resource);
            _uiBindings.Add(evt.ActorId, uiInstance);
            DebugUtility.LogVerbose<BindHandler>($"OnResourceBind: Created ResourceUI for ActorId={evt.ActorId}, ResourceId={resourceId}, Type={evt.Type}, Source={evt.Source.name}, UI Source={uiInstance.gameObject.name}");
        }

        public ResourceUI GetResourceUI(string actorId)
        {
            return _uiBindings.TryGetValue(actorId, out var ui) ? ui : null;
        }

        private void OnDestroy()
        {
            _uiBindings.Clear();
            DebugUtility.LogVerbose<BindHandler>($"OnDestroy: Cleared bindings dictionary, Source={gameObject.name}");
        }
    }
}