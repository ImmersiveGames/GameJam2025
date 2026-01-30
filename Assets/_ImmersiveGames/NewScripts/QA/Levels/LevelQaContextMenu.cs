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

        private const string ReasonSelect = "QA/Level/Select";
        private const string ReasonApply = "QA/Level/Apply";
        private const string ReasonNavigate = "QA/Level/Navigate";
        private const string ReasonDump = "QA/Level/Dump";

        [SerializeField] private string levelId = string.Empty;

        [ContextMenu("QA/Level/SelectLevel - Resolve Catalog")]
        private void Qa_SelectLevel()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(levelId))
            {
                if (service.SelectInitialLevel(ReasonSelect))
                {
                    DebugUtility.Log(typeof(LevelQaContextMenu),
                        "[QA][LevelManager] SelectLevel resolveu nível inicial via catálogo.",
                        ColorOk);
                }
                else
                {
                    DebugUtility.Log(typeof(LevelQaContextMenu),
                        "[QA][LevelManager] SelectLevel falhou ao resolver nível inicial.",
                        ColorWarn);
                }

                return;
            }

            if (service.SelectLevel(levelId.Trim(), ReasonSelect))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][LevelManager] SelectLevel selecionado levelId='{levelId}'.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][LevelManager] SelectLevel falhou levelId='{levelId}'.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/ApplyLevel - Apply Selected")]
        private void Qa_ApplyLevel()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                "[QA][LevelManager] ApplyLevel solicitado.",
                ColorInfo);

            _ = service.ApplySelectedLevelAsync(ReasonApply);
        }

        [ContextMenu("QA/Level/Next - Select Next")]
        private void Qa_SelectNext()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            if (service.SelectNextLevel(ReasonNavigate))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Next selecionado (catálogo).",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Next falhou (catálogo).",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/Previous - Select Previous")]
        private void Qa_SelectPrevious()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            if (service.SelectPreviousLevel(ReasonNavigate))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Previous selecionado (catálogo).",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Previous falhou (catálogo).",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/DumpCurrent - Current Snapshot")]
        private void Qa_DumpCurrent()
        {
            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null)
            {
                return;
            }

            service.DumpCurrent(ReasonDump);
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
