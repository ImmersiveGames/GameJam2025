using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(-100)]
    public class ActorResourceOrchestrator : MonoBehaviour
    {
        private static ActorResourceOrchestrator _instance;
        public static ActorResourceOrchestrator Instance => _instance;

        // Agora guardamos diretamente a interface do sistema de recursos
        private readonly Dictionary<IActor, IEntityResourceSystem> _actors = new();
        private readonly Dictionary<string, CanvasResourceBinder> _canvases = new();
        private readonly Dictionary<string, List<CanvasResourceBinder>> _canvasesByScene = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"🔄 Cena carregada: {scene.name}");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"🔓 Cena descarregada: {scene.name}");

            if (_canvasesByScene.TryGetValue(scene.name, out var sceneCanvases))
            {
                foreach (var canvas in sceneCanvases)
                {
                    if (canvas != null)
                    {
                        _canvases.Remove(canvas.CanvasId);
                        DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                            $"🔓 Canvas removido (cena descarregada): {canvas.CanvasId}");
                    }
                }
                _canvasesByScene.Remove(scene.name);
            }
        }

        // Registro do actor -> agora recebe a interface do resource system
        public void RegisterActor(IActor actor, IEntityResourceSystem resourceSystem)
        {
            if (actor == null || resourceSystem == null) return;

            if (_actors.ContainsKey(actor))
            {
                DebugUtility.LogWarning<ActorResourceOrchestrator>(
                    $"⚠️ Actor já registrado: {actor.ActorName}");
                return;
            }

            _actors[actor] = resourceSystem;
            DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                $"🎯 Actor registrado: {actor.ActorName}");

            CreateActorSlotsInAllCanvases(actor, resourceSystem);
        }

        public void RegisterCanvas(CanvasResourceBinder canvasBinder)
        {
            if (canvasBinder == null) return;

            string sceneName = canvasBinder.gameObject.scene.name;

            if (!_canvasesByScene.ContainsKey(sceneName))
                _canvasesByScene[sceneName] = new List<CanvasResourceBinder>();

            _canvasesByScene[sceneName].Add(canvasBinder);
            _canvases[canvasBinder.CanvasId] = canvasBinder;

            DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                $"🎨 Canvas registrado: {canvasBinder.CanvasId} na cena {sceneName}");

            CreateAllActorSlotsInCanvas(canvasBinder);
        }

        private void CreateActorSlotsInAllCanvases(IActor actor, IEntityResourceSystem resourceSystem)
        {
            foreach (var canvas in _canvases.Values)
            {
                CreateActorSlotsInCanvas(actor, resourceSystem, canvas);
            }
        }

        private void CreateAllActorSlotsInCanvas(CanvasResourceBinder canvas)
        {
            foreach (var actorPair in _actors)
            {
                CreateActorSlotsInCanvas(actorPair.Key, actorPair.Value, canvas);
            }
        }

        private void CreateActorSlotsInCanvas(IActor actor, IEntityResourceSystem resourceSystem, CanvasResourceBinder canvas)
        {
            var resources = resourceSystem.GetAllResources();
            string targetCanvasId = canvas.CanvasId;

            foreach (var resource in resources)
            {
                string resourceTargetCanvas = resourceSystem.GetTargetCanvasId(resource.Key);
                if (resourceTargetCanvas == targetCanvasId)
                {
                    canvas.CreateSlotForActor(actor, resource.Key, resource.Value);
                }
            }
        }

        public void UnregisterActor(IActor actor)
        {
            if (_actors.TryGetValue(actor, out _))
            {
                foreach (var canvasBinder in _canvases.Values)
                {
                    canvasBinder.RemoveSlotsForActor(actor);
                }
                _actors.Remove(actor);
                DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                    $"🔓 Actor removido: {actor.ActorName}");
            }
        }

        public void UnregisterCanvas(CanvasResourceBinder canvasBinder)
        {
            if (canvasBinder != null)
            {
                string sceneName = canvasBinder.gameObject.scene.name;

                if (_canvasesByScene.TryGetValue(sceneName, out var sceneCanvases))
                {
                    sceneCanvases.Remove(canvasBinder);
                    if (sceneCanvases.Count == 0)
                        _canvasesByScene.Remove(sceneName);
                }

                _canvases.Remove(canvasBinder.CanvasId);
                DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                    $"🔓 Canvas removido: {canvasBinder.CanvasId}");
            }
        }

        public void UpdateResourceForActor(IActor actor, ResourceType resourceType, IResourceValue data)
        {
            if (actor == null || !_actors.ContainsKey(actor)) return;

            var resourceSystem = _actors[actor];
            string targetCanvasId = resourceSystem.GetTargetCanvasId(resourceType);

            if (_canvases.TryGetValue(targetCanvasId, out var canvas))
            {
                canvas.UpdateResourceForActor(actor, resourceType, data);
            }
            else
            {
                DebugUtility.LogWarning<ActorResourceOrchestrator>(
                    $"⚠️ Canvas alvo não encontrado: {targetCanvasId} para {actor.ActorName}.{resourceType}");
            }
        }

        public void CreateSlotsForActorInScene(IActor actor, string sceneName)
        {
            if (_actors.TryGetValue(actor, out var resourceSystem) &&
                _canvasesByScene.TryGetValue(sceneName, out var sceneCanvases))
            {
                foreach (var canvas in sceneCanvases)
                {
                    CreateActorSlotsInCanvas(actor, resourceSystem, canvas);
                }
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        [ContextMenu("Debug State")]
        public void DebugState()
        {
            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"=== Orchestrator State ===");
            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"Actores: {_actors.Count}");
            foreach (var actor in _actors.Keys)
            {
                DebugUtility.LogVerbose<ActorResourceOrchestrator>($"  - {actor.ActorName}");
            }

            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"Canvases por cena:");
            foreach (var scenePair in _canvasesByScene)
            {
                DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                    $"  {scenePair.Key}: {scenePair.Value.Count} canvases");
                foreach (var canvas in scenePair.Value)
                {
                    DebugUtility.LogVerbose<ActorResourceOrchestrator>(
                        $"    - {canvas.CanvasId}");
                }
            }
            DebugUtility.LogVerbose<ActorResourceOrchestrator>($"=== Fim do State ===");
        }
    }
}
