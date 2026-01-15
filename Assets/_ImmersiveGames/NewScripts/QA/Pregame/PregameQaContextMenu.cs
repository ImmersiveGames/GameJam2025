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
        [ContextMenu("QA/IntroStage/Complete (Force)")]
        private void CompletePregame()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<PregameQaContextMenu>(
                    "[QA][IntroStage] IPregameControlService indisponível; Complete ignorado.");
                return;
            }

            controlService.CompletePregame("QA/IntroStage/Complete");
            DebugUtility.Log<PregameQaContextMenu>(
                "[QA][IntroStage] CompletePregame solicitado.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStage/Skip (Force)")]
        private void SkipPregame()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<PregameQaContextMenu>(
                    "[QA][IntroStage] IPregameControlService indisponível; Skip ignorado.");
                return;
            }

            controlService.SkipPregame("QA/IntroStage/Skip");
            DebugUtility.Log<PregameQaContextMenu>(
                "[QA][IntroStage] SkipPregame solicitado.",
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
