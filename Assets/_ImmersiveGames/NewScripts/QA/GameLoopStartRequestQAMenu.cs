using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStartRequestQAMenu : MonoBehaviour
    {
        [ContextMenu("QA/GameLoop/REQUEST Start (GameStartRequestedEvent)")]
        private void QA_RequestStart()
        {
            DebugUtility.Log<GameLoopStartRequestQAMenu>(
                "[QA] Emitting GameStartRequestedEvent (REQUEST).",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
