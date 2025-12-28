// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
﻿#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.QA
{
    /// <summary>
    /// QA ONLY:
    /// Emite GameStartRequestedEvent (REQUEST) para iniciar o fluxo (SceneFlow) em builds de QA/dev.
    ///
    /// Importante:
    /// - Este script NÃO deve emitir COMMAND de start (isso é responsabilidade do Coordinator após readiness).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class GameLoopStartRequestQaFrontend : MonoBehaviour
    {
        [ContextMenu("QA/GameLoop/Emit Start REQUEST")]
        public void EmitStartRequest()
        {
            DebugUtility.Log(typeof(GameLoopStartRequestQaFrontend),
                "[QA] Emitting GameStartRequestedEvent (REQUEST).",
                DebugUtility.Colors.Info);

            EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
        }
    }
}
#endif
