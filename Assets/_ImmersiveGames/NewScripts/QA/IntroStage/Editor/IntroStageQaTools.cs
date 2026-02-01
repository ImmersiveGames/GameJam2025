using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Editor
{
    public static class IntroStageQaTools
    {
        private const string QaGameObjectName = "QA_IntroStage";

        [MenuItem("Tools/NewScripts/QA/Select QA_IntroStage")]
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
    }
}
