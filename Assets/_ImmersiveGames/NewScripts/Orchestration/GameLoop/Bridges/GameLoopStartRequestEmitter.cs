using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges
{
    /// <summary>
    /// Emite BootStartPlanRequestedEvent (REQUEST) uma unica vez ao iniciar a cena.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class BootStartPlanRequestEmitter : MonoBehaviour
    {
        private const string EmitterObjectName = "[GameLoop]_BootStartPlanRequestEmitter";

        private static bool _hasRequested;
        private static bool _installed;

        public static void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            var existing = FindFirstObjectByType<BootStartPlanRequestEmitter>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _installed = true;
                return;
            }

            var go = GameObject.Find(EmitterObjectName);
            if (go == null)
            {
                go = new GameObject(EmitterObjectName);
                DontDestroyOnLoad(go);
            }

            if (!go.TryGetComponent<BootStartPlanRequestEmitter>(out _))
            {
                go.AddComponent<BootStartPlanRequestEmitter>();
            }

            _installed = true;

            DebugUtility.LogVerbose(typeof(BootStartPlanRequestEmitter),
                "[OBS][GameLoop] BootStartPlanRequestEmitter ensured in runtime bootstrap.",
                DebugUtility.Colors.Info);
        }

        private void Awake()
        {
            _hasRequested = false;
        }

        private void Start()
        {
            if (_hasRequested)
            {
                return;
            }

            _hasRequested = true;

            DebugUtility.Log<BootStartPlanRequestEmitter>(
                "[OBS][Gameplay] BootStartPlanRequestedEvent emitted for slice rail 'Gameplay -> Level -> EnterStage -> Playing'.",
                DebugUtility.Colors.Info);

            EventBus<BootStartPlanRequestedEvent>.Raise(new BootStartPlanRequestedEvent());
        }
    }
}
