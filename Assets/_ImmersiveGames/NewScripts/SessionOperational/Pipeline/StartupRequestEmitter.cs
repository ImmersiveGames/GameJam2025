using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.RunPipeline.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    /// <summary>
    /// Emite BootStartPlanRequestedEvent uma unica vez ao iniciar a cena.
    /// Seam canonico de bootstrap/startup do rail operacional.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class StartupRequestEmitter : MonoBehaviour
    {
        private const string EmitterObjectName = "[SessionOperationalStartup]_StartupRequestEmitter";

        private static bool _hasRequested;
        private static bool _installed;

        public static void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            var existing = FindFirstObjectByType<StartupRequestEmitter>(FindObjectsInactive.Include);
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

            if (!go.TryGetComponent<StartupRequestEmitter>(out _))
            {
                go.AddComponent<StartupRequestEmitter>();
            }

            _installed = true;

            DebugUtility.LogVerbose(typeof(StartupRequestEmitter),
                "emitter ensured in canonical bootstrap.",
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

            DebugUtility.Log<StartupRequestEmitter>(
                "BootStartPlanRequestedEvent emitted for canonical startup rail.",
                DebugUtility.Colors.Info);

            EventBus<BootStartPlanRequestedEvent>.Raise(new BootStartPlanRequestedEvent());
        }
    }
}
