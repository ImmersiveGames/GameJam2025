using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(-50)]
    [DebugLevel(DebugLevel.Logs)]
    public class CanvasResourceBinder : MonoBehaviour, ICanvasResourceBinder
    {
        [SerializeField] private string canvasId;
        [SerializeField] private ResourceUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private bool persistAcrossScenes = false;

        private readonly Dictionary<IActor, Dictionary<ResourceType, ResourceUISlot>> _dynamicSlots = new();
        private EventBinding<ResourceUpdateEvent> _updateBinding;

        public string CanvasId => canvasId;
        public string SceneName => gameObject.scene.name;

        private void Awake()
        {
            if (string.IsNullOrEmpty(canvasId))
                canvasId = $"{gameObject.scene.name}_{gameObject.name}";

            // Se for persistente, move para DontDestroyOnLoad
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Garante que o orchestrator existe
            if (ActorResourceOrchestrator.Instance == null)
            {
                new GameObject("ActorResourceOrchestrator").AddComponent<ActorResourceOrchestrator>();
            }
            
            ActorResourceOrchestrator.Instance.RegisterCanvas(this);
            RegisterEventListeners();
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 CanvasBinder criado: {canvasId} na cena {SceneName}");
        }

        private void Start()
        {
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 CanvasBinder inicializado: {canvasId}");
        }

        private void RegisterEventListeners()
        {
            _updateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_updateBinding);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            var actor = FindActorById(evt.ActorId);
            if (actor != null)
            {
                UpdateResourceForActor(actor, evt.ResourceType, evt.NewValue);
            }
        }

        private IActor FindActorById(string actorId)
        {
            return _dynamicSlots.Keys.FirstOrDefault(actor => 
                actor.ActorName.Equals(actorId, System.StringComparison.OrdinalIgnoreCase));
        }

        public void CreateSlotForActor(IActor actor, ResourceType resourceType, IResourceValue data)
        {
            if (actor == null || slotPrefab == null) 
            {
                DebugUtility.LogWarning<CanvasResourceBinder>($"❌ Não pode criar slot em {canvasId}");
                return;
            }

            // Verifica se o slot já existe
            if (_dynamicSlots.TryGetValue(actor, out var actorSlots) && 
                actorSlots.ContainsKey(resourceType))
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"ℹ️ Slot já existe: {actor.ActorName}.{resourceType} em {canvasId}");
                return;
            }

            var slot = Instantiate(slotPrefab, dynamicSlotsParent != null ? dynamicSlotsParent : transform);
            slot.InitializeForActor(actor, resourceType);
            slot.Configure(data);
            
            if (!_dynamicSlots.ContainsKey(actor))
                _dynamicSlots[actor] = new Dictionary<ResourceType, ResourceUISlot>();
                
            _dynamicSlots[actor][resourceType] = slot;
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔗 Slot criado: {actor.ActorName}.{resourceType} em {canvasId}");
        }

        public void UpdateResourceForActor(IActor actor, ResourceType resourceType, IResourceValue data)
        {
            if (_dynamicSlots.TryGetValue(actor, out var actorSlots) && 
                actorSlots.TryGetValue(resourceType, out var slot))
            {
                slot.Configure(data);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔄 Slot atualizado: {actor.ActorName}.{resourceType} em {canvasId}");
            }
            else
            {
                // Se o slot não existe, cria um novo
                CreateSlotForActor(actor, resourceType, data);
            }
        }

        public void RemoveSlotsForActor(IActor actor)
        {
            if (_dynamicSlots.TryGetValue(actor, out var actorSlots))
            {
                foreach (var slot in actorSlots.Values)
                {
                    if (slot != null) Destroy(slot.gameObject);
                }
                _dynamicSlots.Remove(actor);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔓 Slots removidos para: {actor.ActorName} de {canvasId}");
            }
        }

        // Método para quando um actor entra na cena deste canvas
        public void OnActorEnteredScene(IActor actor, Dictionary<ResourceType, IResourceValue> initialResources)
        {
            if (actor == null || initialResources == null) return;

            foreach (var kvp in initialResources)
            {
                CreateSlotForActor(actor, kvp.Key, kvp.Value);
            }
        }

        // Métodos da interface (não usados)
        public bool TryBindActor(string actorId, ResourceType type, IResourceValue data) => false;
        public void UnbindActor(string actorId) { }
        public void UpdateResource(string actorId, ResourceType type, IResourceValue data) { }

        private void OnDestroy()
        {
            if (_updateBinding != null)
                EventBus<ResourceUpdateEvent>.Unregister(_updateBinding);
                
            if (ActorResourceOrchestrator.Instance != null)
            {
                ActorResourceOrchestrator.Instance.UnregisterCanvas(this);
            }
            
            // Limpa todos os slots
            foreach (var actor in _dynamicSlots.Keys.ToList())
            {
                RemoveSlotsForActor(actor);
            }
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"♻️ CanvasBinder destruído: {canvasId}");
        }

        [ContextMenu("Debug Slots")]
        public void DebugSlots()
        {
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 Canvas {canvasId} Slots ({_dynamicSlots.Count} actors):");
            foreach (var actorSlot in _dynamicSlots)
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"   {actorSlot.Key.ActorName}: {actorSlot.Value.Count} slots");
            }
        }
    }
}