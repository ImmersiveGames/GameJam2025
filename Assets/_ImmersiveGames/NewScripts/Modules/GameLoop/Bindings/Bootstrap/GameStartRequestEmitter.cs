using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap
{
    /// <summary>
    /// Emite GameStartRequestedEvent (REQUEST) uma única vez ao iniciar a cena.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameStartRequestEmitter : MonoBehaviour
    {
        private const string EmitterObjectName = "[GameLoop]_GameStartRequestEmitter";

        private static bool _hasRequested;
        private static bool _installed;

        public static void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            var existing = FindFirstObjectByType<GameStartRequestEmitter>(FindObjectsInactive.Include);
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

            if (!go.TryGetComponent<GameStartRequestEmitter>(out _))
            {
                go.AddComponent<GameStartRequestEmitter>();
            }

            _installed = true;

            DebugUtility.LogVerbose(typeof(GameStartRequestEmitter),
                "[OBS][GameLoop] GameStartRequestEmitter ensured in runtime bootstrap.",
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

            DebugUtility.Log(typeof(GameStartRequestEmitter),
                "[Production][StartRequest] Start solicitado (GameStartRequestedEvent).",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
