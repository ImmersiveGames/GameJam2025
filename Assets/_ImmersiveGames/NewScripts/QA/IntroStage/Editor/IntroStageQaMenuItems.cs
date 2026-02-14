using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.IntroStage.Editor
{
    public static class IntroStageQaMenuItems
    {
        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Complete (Force)", priority = 1200)]
        private static void CompleteIntroStage()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStage] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStage] IIntroStageControlService indisponível; Complete ignorado.");
                return;
            }

            service.CompleteIntroStage("QA/MenuItem/Complete");
            DebugUtility.Log(typeof(IntroStageQaMenuItems),
                "[QA][IntroStage] MenuItem CompleteIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Skip (Force)", priority = 1201)]
        private static void SkipIntroStage()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStage] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStage] IIntroStageControlService indisponível; Skip ignorado.");
                return;
            }

            service.SkipIntroStage("QA/MenuItem/Skip");
            DebugUtility.Log(typeof(IntroStageQaMenuItems),
                "[QA][IntroStage] MenuItem SkipIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        // Mantém o comando habilitado apenas durante o Play Mode.
        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Complete (Force)", true, 1200)]
        private static bool ValidateCompleteIntroStage()
        {
            return Application.isPlaying;
        }

        // Mantém o comando habilitado apenas durante o Play Mode.
        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Skip (Force)", true, 1201)]
        private static bool ValidateSkipIntroStage()
        {
            return Application.isPlaying;
        }

    }
}
