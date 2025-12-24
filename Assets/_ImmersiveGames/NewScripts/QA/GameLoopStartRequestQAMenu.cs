#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameLoop.QA
{
    /// <summary>
    /// QA ONLY:
    /// Emite GameStartRequestedEvent para iniciar o fluxo (SceneFlow) em builds de QA/dev.
    ///
    /// TODO(PROD): Remover/encapsular atrás de define/flag antes de produção.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStartRequestQAMenu : MonoBehaviour
    {
        [ContextMenu("QA/Emit Start REQUEST")]
        public void EmitStartRequest()
        {
            DebugUtility.Log(typeof(GameLoopStartRequestQAMenu),
                "[QA] Emitting GameStartRequestedEvent (REQUEST).",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
#endif
