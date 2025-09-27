using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(-10)]
    [DebugLevel(DebugLevel.Logs)]
    public class EntityResourceBinder : MonoBehaviour
    {
        [Header("Dynamic Spawn Settings")]
        [SerializeField] private bool autoRegister = true;

        private IActor _actor;
        private EntityResourceSystem _resourceSystem;

        public EntityResourceSystem ResourceSystem => _resourceSystem;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            _resourceSystem = GetComponent<EntityResourceSystem>();

            if (_actor == null)
                _actor = GetComponentInParent<IActor>();
        }

        private void Start()
        {
            if (!autoRegister || _actor == null || _resourceSystem == null)
                return;

            // Garante que recursos existam antes do registro
            if (!_resourceSystem.IsInitialized)
            {
                DebugUtility.LogVerbose<EntityResourceBinder>(
                    $"⚡ Forçando inicialização de recursos para {_actor.ActorName}");
                _resourceSystem.InitializeResources();
            }

            RegisterWithOrchestrator();
        }

        public void RegisterWithOrchestrator()
        {
            if (_actor == null || _resourceSystem == null) return;

            if (ActorResourceOrchestrator.Instance == null)
            {
                new GameObject("ActorResourceOrchestrator").AddComponent<ActorResourceOrchestrator>();
            }

            // Nota: passamos a interface, que o Orchestrator agora espera
            ActorResourceOrchestrator.Instance.RegisterActor(_actor, _resourceSystem);
            DebugUtility.LogVerbose<EntityResourceBinder>($"🎯 EntityBinder registrado: {_actor.ActorName}");
        }

        // Método para quando o actor é spawnado dinamicamente durante o gameplay
        public void OnSpawnedInScene(string sceneName)
        {
            if (ActorResourceOrchestrator.Instance != null)
            {
                ActorResourceOrchestrator.Instance.CreateSlotsForActorInScene(_actor, sceneName);
            }
        }

        private void OnDestroy()
        {
            if (_actor != null && ActorResourceOrchestrator.Instance != null)
            {
                ActorResourceOrchestrator.Instance.UnregisterActor(_actor);
            }
        }

        [ContextMenu("Manual Register")]
        public void ManualRegister()
        {
            RegisterWithOrchestrator();
        }
    }
}
