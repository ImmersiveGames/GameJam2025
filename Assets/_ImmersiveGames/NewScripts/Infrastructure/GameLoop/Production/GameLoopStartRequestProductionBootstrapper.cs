using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop.Production
{
    /// <summary>
    /// PRODUÇÃO:
    /// Emite GameStartRequestedEvent (REQUEST) automaticamente ao iniciar a cena de bootstrap.
    /// Isso permite o Coordinator disparar o startPlan (NewBootstrap -> Menu/UIGlobal) sem depender de QA.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStartRequestProductionBootstrapper : MonoBehaviour
    {
        private const string BootstrapSceneName = "NewBootstrap";
        private static bool _requested;

        [SerializeField]
        private bool autoRequestOnStart = true;

        private void Start()
        {
#if !NEWSCRIPTS_MODE
            return;
#endif
            if (!autoRequestOnStart)
                return;

            // Segurança: só dispara no bootstrap.
            if (gameObject.scene.name != BootstrapSceneName)
                return;

            if (_requested)
                return;

            _requested = true;

            DebugUtility.Log(typeof(GameLoopStartRequestProductionBootstrapper),
                "[Production] Emitting GameStartRequestedEvent (REQUEST) for startup SceneFlow.",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
