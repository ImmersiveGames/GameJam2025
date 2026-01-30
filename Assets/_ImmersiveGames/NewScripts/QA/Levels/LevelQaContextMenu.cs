// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaContextMenu.cs
// QA de LevelManager: ações objetivas para evidência.

#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Levels;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Levels
{
    public sealed class LevelQaContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        private const string ReasonApply = "QA/Level/Apply";
        private const string ReasonSelectLevel1 = "QA/Level/Select/level.1";
        private const string ReasonSelectLevel2 = "QA/Level/Select/level.2";
        private const string ReasonPrintStatus = "QA/Level/PrintStatus";
        private const string ReasonClearSelection = "QA/Level/ClearSelection";

        [ContextMenu("QA/Level/Select Level 1")]
        private void Qa_SelectLevel1()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            if (service.SelectLevel("level.1", ReasonSelectLevel1))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Level 1 aplicado.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Level 1 falhou.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/Select Level 2")]
        private void Qa_SelectLevel2()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            if (service.SelectLevel("level.2", ReasonSelectLevel2))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Level 2 aplicado.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Level 2 falhou.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/Apply Selected")]
        private void Qa_ApplyLevel()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                "[QA][LevelManager] Apply Selected solicitado.",
                ColorInfo);

            _ = service.ApplySelectedLevelAsync(ReasonApply);
        }

        [ContextMenu("QA/Level/Print Status")]
        private void Qa_PrintStatus()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            service.DumpCurrent(ReasonPrintStatus);
        }

        [ContextMenu("QA/Level/Clear Selection")]
        private void Qa_ClearSelection()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            service.ClearSelection(ReasonClearSelection);

            DebugUtility.Log(typeof(LevelQaContextMenu),
                "[QA][LevelManager] Clear Selection solicitado.",
                ColorInfo);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/NewScripts/QA/Level/Select QA_Level Object", priority = 11)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_Level");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu), "[QA][LevelManager] QA_Level não encontrado no Hierarchy (Play Mode).", ColorWarn);
            }
        }
#endif

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][LevelManager] Serviço global ausente: {label}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
