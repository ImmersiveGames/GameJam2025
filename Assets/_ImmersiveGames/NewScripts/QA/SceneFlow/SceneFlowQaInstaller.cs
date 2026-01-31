// Assets/_ImmersiveGames/NewScripts/QA/SceneFlow/SceneFlowQaInstaller.cs
// Instalador do QA de SceneFlow/WorldLifecycle em Editor/Development.

#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.SceneFlow
{
    /// <summary>
    /// Garante que exista um GameObject com o componente SceneFlowQaContextMenu no Play Mode.
    /// </summary>
    public static class SceneFlowQaInstaller
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

            var existing = Object.FindFirstObjectByType<SceneFlowQaContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(SceneFlowQaInstaller), "[QA][SceneFlow] SceneFlowQaContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(SceneFlowQaInstaller), "[QA][SceneFlow] SceneFlowQaContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(SceneFlowQaInstaller), "[QA][SceneFlow] GameObject QA_SceneFlow existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<SceneFlowQaContextMenu>();
            if (menu == null)
            {
                go.AddComponent<SceneFlowQaContextMenu>();
                DebugUtility.Log(typeof(SceneFlowQaInstaller), "[QA][SceneFlow] SceneFlowQaContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(SceneFlowQaInstaller), "[QA][SceneFlow] Para acessar o ContextMenu, selecione o GameObject 'QA_SceneFlow' no Hierarchy (DontDestroyOnLoad).", ColorOk);
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
