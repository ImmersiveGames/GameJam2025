#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Dev
{
    /// <summary>
    /// Garante que exista um GameObject com o componente LevelDevContextMenu no Play Mode.
    /// </summary>
    public static class LevelDevInstaller
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

            var existing = Object.FindFirstObjectByType<LevelDevContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(LevelDevInstaller), "[QA][Level] LevelDevContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelDevInstaller), "[QA][Level] LevelDevContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelDevInstaller), "[QA][Level] GameObject QA_Level existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<LevelDevContextMenu>();
            if (menu == null)
            {
                go.AddComponent<LevelDevContextMenu>();
                DebugUtility.Log(typeof(LevelDevInstaller), "[QA][Level] LevelDevContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(LevelDevInstaller), "[QA][Level] Para acessar o ContextMenu, selecione o GameObject 'QA_Level' no Hierarchy (DontDestroyOnLoad).", ColorOk);
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
