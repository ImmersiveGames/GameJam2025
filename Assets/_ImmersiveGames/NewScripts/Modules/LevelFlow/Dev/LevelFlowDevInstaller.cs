#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Dev
{
    /// <summary>
    /// Garante que exista um GameObject com o componente LevelFlowDevContextMenu no Play Mode.
    /// </summary>
    public static class LevelFlowDevInstaller
    {
        private const string QaObjectName = "QA_LevelFlow";

        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorVerbose = "#00BCD4";

        public static void EnsureInstalled()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existing = Object.FindFirstObjectByType<LevelFlowDevContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(LevelFlowDevInstaller),
                    "[QA][LevelFlow] LevelFlowDevContextMenu já instalado (instância existente).",
                    ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelFlowDevInstaller),
                    "[QA][LevelFlow] LevelFlowDevContextMenu instalado (DontDestroyOnLoad).",
                    ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(LevelFlowDevInstaller),
                    "[QA][LevelFlow] GameObject QA_LevelFlow existente; reaproveitando e marcando DontDestroyOnLoad.",
                    ColorVerbose);
            }

            var menu = go.GetComponent<LevelFlowDevContextMenu>();
            if (menu == null)
            {
                go.AddComponent<LevelFlowDevContextMenu>();
                DebugUtility.Log(typeof(LevelFlowDevInstaller),
                    "[QA][LevelFlow] LevelFlowDevContextMenu ausente; componente adicionado.",
                    ColorInfo);
            }

            DebugUtility.Log(typeof(LevelFlowDevInstaller),
                "[QA][LevelFlow] Para acessar o ContextMenu, selecione o GameObject 'QA_LevelFlow' no Hierarchy (DontDestroyOnLoad).",
                ColorOk);
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
