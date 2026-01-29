#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.IntroStage
{
    public sealed class IntroStageQaContextMenu : MonoBehaviour
    {
        [ContextMenu("QA/IntroStage/Complete (Force)")]
        private void CompleteIntroStage()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<IntroStageQaContextMenu>(
                    "[QA][IntroStage] IIntroStageControlService indisponível; Complete ignorado.");
                return;
            }

            controlService.CompleteIntroStage("IntroStage/UIConfirm");
            DebugUtility.Log<IntroStageQaContextMenu>(
                "[QA][IntroStage] CompleteIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStage/Skip (Force)")]
        private void SkipIntroStage()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<IntroStageQaContextMenu>(
                    "[QA][IntroStage] IIntroStageControlService indisponível; Skip ignorado.");
                return;
            }

            controlService.SkipIntroStage("QA/IntroStage/Skip");
            DebugUtility.Log<IntroStageQaContextMenu>(
                "[QA][IntroStage] SkipIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStage/Dump - Status")]
        private void DumpStatus()
        {
            var controlService = ResolveControlService();
            var gameLoopState = ResolveGameLoopStateName();

            if (controlService == null)
            {
                DebugUtility.LogWarning<IntroStageQaContextMenu>(
                    $"[QA][IntroStage] DumpStatus: IIntroStageControlService indisponível. gameLoopState='{gameLoopState}'.");
                return;
            }

            DebugUtility.Log<IntroStageQaContextMenu>(
                $"[QA][IntroStage] DumpStatus: controlService=resolved isActive={controlService.IsIntroStageActive.ToString().ToLowerInvariant()} " +
                $"gameLoopState='{gameLoopState}'.",
                DebugUtility.Colors.Info);
        }

        private static IIntroStageControlService? ResolveControlService()
        {
            return DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service)
                ? service
                : null;
        }

        private static string ResolveGameLoopStateName()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) && gameLoop != null
                ? gameLoop.CurrentStateIdName
                : "<none>";
        }
    }
}
