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

        private const string ReasonSelectInitial = "QA/Level/Select/initial";
        private const string ReasonApply = "QA/Level/Apply";
        private const string ReasonSelectLevel1 = "QA/Level/Select/level.1";
        private const string ReasonSelectLevel2 = "QA/Level/Select/level.2";

        [ContextMenu("QA/Level/L01 Select Initial")]
        private void Qa_SelectInitial()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null || !EnsureCatalogReady())
            {
                return;
            }

            if (service.SelectInitialLevel(ReasonSelectInitial))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Initial aplicado.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select Initial falhou.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/L02 Apply Selected InPlace")]
        private void Qa_ApplySelected()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null || !EnsureCatalogReady())
            {
                return;
            }

            if (!service.SelectInitialLevel(ReasonSelectInitial))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Apply Selected abortado (falha ao selecionar initial).",
                    ColorWarn);
                return;
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                "[QA][LevelManager] Apply Selected solicitado.",
                ColorInfo);

            _ = service.ApplySelectedLevelAsync(ReasonApply);
        }

        [ContextMenu("QA/Level/L03 Select level.1")]
        private void Qa_SelectLevel1()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null || !EnsureCatalogReady())
            {
                return;
            }

            if (service.SelectLevel("level.1", ReasonSelectLevel1))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select level.1 aplicado.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select level.1 falhou.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Level/L04 Select level.2")]
        private void Qa_SelectLevel2()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var service = ResolveGlobal<ILevelManagerService>("ILevelManagerService");
            if (service == null || !EnsureCatalogReady())
            {
                return;
            }

            if (service.SelectLevel("level.2", ReasonSelectLevel2))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select level.2 aplicado.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Select level.2 falhou.",
                    ColorWarn);
            }
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

        private static bool EnsureGateEnabled()
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<Infrastructure.Promotion.PromotionGateService>(out var gate) || gate == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] PromotionGateService ausente; prosseguindo por default.",
                    ColorWarn);
                return true;
            }

            if (!gate.IsEnabled(string.Empty))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] Gate de promoção desabilitado; ação QA ignorada.",
                    ColorWarn);
                return false;
            }

            return true;
        }

        private static bool EnsureCatalogReady()
        {
            var resolver = ResolveGlobal<_ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers.ILevelCatalogResolver>("ILevelCatalogResolver");
            if (resolver == null)
            {
                return false;
            }

            if (!resolver.TryResolveCatalog(out var catalog) || catalog == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] LevelCatalog ausente (Resources).",
                    ColorWarn);
                return false;
            }

            if (catalog.Definitions == null || catalog.Definitions.Count == 0)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] LevelCatalog sem definitions.",
                    ColorWarn);
                return false;
            }

            if (string.IsNullOrWhiteSpace(catalog.InitialLevelId))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][LevelManager] LevelCatalog sem initialLevelId válido.",
                    ColorWarn);
                return false;
            }

            return true;
        }
    }
}
