#if UNITY_EDITOR
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Editor
{
    public static class IntroStageQaMenuItems
    {
        private const string QaGameObjectName = "QA_IntroStage";
        private const string SelectQaMenuPath = "ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Select QA_IntroStage Object";

        [MenuItem(SelectQaMenuPath, priority = 1290)]
        private static void SelectQaIntroStage()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[QA][IntroStageController] Selecao requer Play Mode.");
                return;
            }

            GameObject go = GameObject.Find(QaGameObjectName);
            if (go == null)
            {
                Debug.LogWarning("[QA][IntroStageController] QA_IntroStage nao encontrado. Verifique o log de instalacao do QA.");
                return;
            }

            Selection.activeObject = go;
        }

        [MenuItem(SelectQaMenuPath, true, 1290)]
        private static bool ValidateSelectQaIntroStage()
        {
            return Application.isPlaying;
        }

        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Complete (Force)", priority = 1291)]
        private static void CompleteIntroStage()
        {
            if (!EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStageController] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStageController] IIntroStageControlService indisponivel; Complete ignorado.");
                return;
            }

            service.CompleteIntroStage("QA/MenuItem/Complete");
            DebugUtility.Log(typeof(IntroStageQaMenuItems),
                "[QA][IntroStageController] MenuItem CompleteIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }

        [MenuItem("ImmersiveGames/NewScripts/QA/GameLoop/IntroStage/Skip (Force)", priority = 1292)]
        private static void SkipIntroStage()
        {
            if (!EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStageController] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQaMenuItems),
                    "[QA][IntroStageController] IIntroStageControlService indisponivel; Skip ignorado.");
                return;
            }

            service.SkipIntroStage("QA/MenuItem/Skip");
            DebugUtility.Log(typeof(IntroStageQaMenuItems),
                "[QA][IntroStageController] MenuItem SkipIntroStage solicitado.",
                DebugUtility.Colors.Info);
        }
    }
}
#endif
