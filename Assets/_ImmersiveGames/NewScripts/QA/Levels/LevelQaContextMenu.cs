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

        private const string ReasonResolveInitial = "QA/Levels/Resolve/InitialLevelId";
        private const string ReasonGoToLevel1 = "QA/Levels/L01-GoToLevel_Level1";
        private const string ReasonGoToLevel2 = "QA/Levels/L01-GoToLevel_Level2";

        [ContextMenu("QA/Levels/Resolve/InitialLevelId")]
        private void Qa_ResolveInitialLevelId()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var resolver = ResolveGlobal<_ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers.ILevelCatalogResolver>("ILevelCatalogResolver");
            if (resolver == null)
            {
                return;
            }

            if (resolver.TryResolveInitialLevelId(out var levelId))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Levels] InitialLevelId resolved levelId='{levelId}' reason='{ReasonResolveInitial}'.",
                    ColorOk);
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Levels] InitialLevelId resolve failed reason='{ReasonResolveInitial}'.",
                    ColorWarn);
            }
        }

        [ContextMenu("QA/Levels/L01-GoToLevel_Level1")]
        private void Qa_GoToLevel1()
        {
            GoToLevelById("level.1", ReasonGoToLevel1);
        }

        [ContextMenu("QA/Levels/L01-GoToLevel_Level2")]
        private void Qa_GoToLevel2()
        {
            GoToLevelById("level.2", ReasonGoToLevel2);
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

        private void GoToLevelById(string levelId, string reason)
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var resolver = ResolveGlobal<_ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers.ILevelCatalogResolver>("ILevelCatalogResolver");
            if (resolver == null)
            {
                return;
            }

            if (!resolver.TryResolvePlan(levelId, out var plan, out var options))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Levels] GoToLevel falhou levelId='{levelId}' reason='{reason}'.",
                    ColorWarn);
                return;
            }

            var levelManager = ResolveGlobal<_ImmersiveGames.NewScripts.Gameplay.Levels.ILevelManager>("ILevelManager");
            if (levelManager == null)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Levels] GoToLevel solicitado levelId='{plan.LevelId}' contentId='{plan.ContentId}' reason='{reason}'.",
                ColorInfo);

            _ = levelManager.RequestLevelInPlaceAsync(plan, reason, options);
        }
    }
}
