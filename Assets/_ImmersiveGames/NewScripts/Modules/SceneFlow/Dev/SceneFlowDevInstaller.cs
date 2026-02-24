#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Dev
{
    /// <summary>
    /// Garante que exista um GameObject com o componente SceneFlowDevContextMenu no Play Mode.
    /// </summary>
    public static class SceneFlowDevInstaller
    {
        private const string QaObjectName = "QA_SceneFlow";

        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorVerbose = "#00BCD4";

        public static void EnsureInstalled()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existing = Object.FindFirstObjectByType<SceneFlowDevContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(SceneFlowDevInstaller), "[QA][SceneFlow] SceneFlowDevContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(SceneFlowDevInstaller), "[QA][SceneFlow] SceneFlowDevContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(SceneFlowDevInstaller), "[QA][SceneFlow] GameObject QA_SceneFlow existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<SceneFlowDevContextMenu>();
            if (menu == null)
            {
                go.AddComponent<SceneFlowDevContextMenu>();
                DebugUtility.Log(typeof(SceneFlowDevInstaller), "[QA][SceneFlow] SceneFlowDevContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(SceneFlowDevInstaller), "[QA][SceneFlow] Para acessar o ContextMenu, selecione o GameObject 'QA_SceneFlow' no Hierarchy (DontDestroyOnLoad).", ColorOk);
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
