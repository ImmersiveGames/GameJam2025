using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev.Editor
{
    public static class IntroStageDevTools
    {
        private const string QaGameObjectName = "QA_IntroStage";

        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Select QA_IntroStage Object", priority = 1290)]
        private static void SelectQaIntroStage()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[QA][IntroStageController] Selecao requer Play Mode.");
                return;
            }

            var go = GameObject.Find(QaGameObjectName);
            if (go == null)
            {
                Debug.LogWarning("[QA][IntroStageController] QA_IntroStage nao encontrado. Verifique o log de instalacao do QA.");
                return;
            }

            Selection.activeObject = go;
        }

        // Mantém o comando habilitado apenas durante o Play Mode.
        [MenuItem("ImmersiveGames/NewScripts/QA/LevelFlow/IntroStage/Select QA_IntroStage Object", true, 1290)]
        private static bool ValidateSelectQaIntroStage()
        {
            return Application.isPlaying;
        }

    }
}
