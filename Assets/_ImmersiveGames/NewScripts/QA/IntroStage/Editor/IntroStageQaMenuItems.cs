using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.QA.IntroStage.Editor
{
    public static class IntroStageQaMenuItems
    {
        [MenuItem("Tools/NewScripts/QA/IntroStage/Complete (Force)")]
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

        [MenuItem("Tools/NewScripts/QA/IntroStage/Skip (Force)")]
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
    }
}
