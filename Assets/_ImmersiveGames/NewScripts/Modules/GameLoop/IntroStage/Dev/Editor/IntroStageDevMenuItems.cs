using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEditor;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev.Editor
{
    public static class IntroStageDevMenuItems
    {
        [MenuItem("Tools/NewScripts/QA/IntroStageController/Complete (Force)")]
        private static void CompleteIntroStage()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageDevMenuItems),
                    "[QA][IntroStageController] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageDevMenuItems),
                    "[QA][IntroStageController] IIntroStageControlService indisponível; Complete ignorado.");
                return;
            }

            service.CompleteIntroStage("QA/MenuItem/Complete");
            DebugUtility.Log(typeof(IntroStageDevMenuItems),
                "[QA][IntroStageController] MenuItem CompleteIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        [MenuItem("Tools/NewScripts/QA/IntroStageController/Skip (Force)")]
        private static void SkipIntroStage()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageDevMenuItems),
                    "[QA][IntroStageController] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageDevMenuItems),
                    "[QA][IntroStageController] IIntroStageControlService indisponível; Skip ignorado.");
                return;
            }

            service.SkipIntroStage("QA/MenuItem/Skip");
            DebugUtility.Log(typeof(IntroStageDevMenuItems),
                "[QA][IntroStageController] MenuItem SkipIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }
    }
}
