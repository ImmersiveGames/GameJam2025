// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaInstaller.cs
// Installer do QA de LevelManager (Baseline 2.2).
// Comentários PT; código EN.

#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Levels
{
    public static class LevelQaInstaller
    {
        private const string QaObjectName = "QA_Level";

        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorVerbose = "#00BCD4";

        public static void EnsureInstalled()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existing = Object.FindObjectOfType<LevelQaContextMenu>(true);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(LevelQaInstaller), "[QA][Level] LevelQaContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelQaInstaller), "[QA][Level] LevelQaContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelQaInstaller), "[QA][Level] GameObject QA_Level existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<LevelQaContextMenu>();
            if (menu == null)
            {
                go.AddComponent<LevelQaContextMenu>();
                DebugUtility.Log(typeof(LevelQaInstaller), "[QA][Level] LevelQaContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(LevelQaInstaller), "[QA][Level] Para acessar o ContextMenu, selecione o GameObject 'QA_Level' no Hierarchy (DontDestroyOnLoad).", ColorOk);
        }

        private static void EnsureName(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (go.name != QaObjectName)
            {
                go.name = QaObjectName;
            }
        }
    }
}
