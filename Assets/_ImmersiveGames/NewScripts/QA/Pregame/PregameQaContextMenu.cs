#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Pregame
{
    [DisallowMultipleComponent]
    public sealed class PregameQaContextMenu : MonoBehaviour
    {
        [ContextMenu("QA/Pregame/Complete (Exit Pregame -> Start)")]
        private void CompletePregame()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<PregameQaContextMenu>(
                    "[QA][Pregame] IPregameControlService indisponível; Complete ignorado.");
                return;
            }

            controlService.CompletePregame("QA/Pregame/Complete");
            DebugUtility.Log<PregameQaContextMenu>(
                "[QA][Pregame] CompletePregame solicitado.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/Pregame/Skip (Exit Pregame -> Start)")]
        private void SkipPregame()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<PregameQaContextMenu>(
                    "[QA][Pregame] IPregameControlService indisponível; Skip ignorado.");
                return;
            }

            controlService.SkipPregame("QA/Pregame/Skip");
            DebugUtility.Log<PregameQaContextMenu>(
                "[QA][Pregame] SkipPregame solicitado.",
                DebugUtility.Colors.Info);
        }

        private static IPregameControlService? ResolveControlService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var service)
                ? service
                : null;
        }
    }
}
