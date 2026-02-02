using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.Bootstrap.Production
{
    /// <summary>
    /// PRODUÇÃO:
    /// Emite GameStartRequestedEvent (REQUEST) uma única vez ao iniciar a cena.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameStartRequestProductionBootstrapper : MonoBehaviour
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

            DebugUtility.Log(typeof(GameStartRequestProductionBootstrapper),
                "[Production][StartRequest] Start solicitado (GameStartRequestedEvent).",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
