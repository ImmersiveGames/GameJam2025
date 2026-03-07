#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap
{
    /// <summary>
    /// LEGACY/DevQA:
    /// emissor de GameStartRequestedEvent mantido apenas para diagnostico em dev.
    /// O trilho canônico de start permanece no GameLoopSceneFlowCoordinator.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameStartRequestEmitter : MonoBehaviour
    {
        private static bool _hasRequested;

        public static void EnsureInstalled()
        {
            // Observabilidade: bootstrap automatico legado foi desativado.
            DebugUtility.LogVerbose(typeof(GameStartRequestEmitter),
                "[OBS][LEGACY][DevQA] GameStartRequestEmitter auto-bootstrap disabled; canonical start is GameLoopSceneFlowCoordinator.",
                DebugUtility.Colors.Info);

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
#endif
