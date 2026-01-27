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

            controlService.CompleteIntroStage("QA/IntroStage/Complete");
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

        private static IIntroStageControlService? ResolveControlService()
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaContextMenu),
                    "[QA][IntroStage] DependencyManager.Provider indisponível; serviço não resolvido.");
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service)
                ? service
                : null;
        }
    }
}
