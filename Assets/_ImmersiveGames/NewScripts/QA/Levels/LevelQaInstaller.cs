// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaInstaller.cs
// Instalador do QA de Levels em Editor/Development.

#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Levels
{
    /// <summary>
    /// Garante que exista um GameObject com o componente LevelQaContextMenu no Play Mode.
    /// </summary>
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

            var existing = Object.FindFirstObjectByType<LevelQaContextMenu>(FindObjectsInactive.Include);
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
