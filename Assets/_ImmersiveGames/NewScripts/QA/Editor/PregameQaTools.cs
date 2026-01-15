#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Editor
{
    public static class PregameQaTools
    {
        private const string QaGameObjectName = "QA_Pregame";

        [MenuItem("Tools/NewScripts/QA/Select QA_IntroStage")]
        private static void SelectQaPregame()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[QA][IntroStage] Selecao requer Play Mode.");
                return;
            }

            var go = GameObject.Find(QaGameObjectName);
            if (go == null)
            {
                go = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(obj => obj != null && obj.name == QaGameObjectName);
            }

            if (go == null)
            {
                Debug.LogWarning("[QA][IntroStage] QA_Pregame nao encontrado. Verifique o log de instalacao do QA.");
                return;
            }

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
