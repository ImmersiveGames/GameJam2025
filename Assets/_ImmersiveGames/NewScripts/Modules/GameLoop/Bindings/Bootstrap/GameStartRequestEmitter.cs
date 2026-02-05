using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap
{
    /// <summary>
    /// PRODUÇÃO:
    /// Emite GameStartRequestedEvent (REQUEST) uma única vez ao iniciar a cena.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameStartRequestEmitter : MonoBehaviour
    {
        private static bool _hasRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetGuard()
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
