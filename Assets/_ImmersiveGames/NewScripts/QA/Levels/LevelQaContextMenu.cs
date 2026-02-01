// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaContextMenu.cs
// QA de LevelManager: ações objetivas para evidência.

#nullable enable
using System.Text;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using _ImmersiveGames.NewScripts.Core.DI;
using _ImmersiveGames.NewScripts.Gameplay.Levels;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Infrastructure.Promotion;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Levels
{
    public sealed class LevelQaContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        private const string ReasonResolveDefinitions = "QA/Levels/Resolve/Definitions";
        private const string ReasonGoToLevel1 = "QA/Levels/L01-GoToLevel";
        private const string ReasonGoToLevel2 = "QA/Levels/L02-GoToLevel";
        private const string ReasonSelectInitial = "QA/Levels/Select/Initial";
        private const string ReasonSelectNext = "QA/Levels/Select/Next";
        private const string ReasonSelectPrev = "QA/Levels/Select/Prev";
        private const string ReasonApplySelected = "QA/Levels/ApplySelected";

        [ContextMenu("QA/Levels/Resolve/Definitions")]
        private void Qa_ResolveDefinitions()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var provider = ResolveGlobal<ILevelCatalogProvider>("ILevelCatalogProvider");
            var resolver = ResolveGlobal<_ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers.ILevelCatalogResolver>("ILevelCatalogResolver");
            if (provider == null || resolver == null)
            {
                return;
            }

            var catalog = provider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Levels] Catalog ausente ao resolver definições reason='{ReasonResolveDefinitions}'.",
                    ColorWarn);
                return;
            }

            var summary = new StringBuilder();
            AppendDefinitionSummary(resolver, summary, "level.1");
            AppendDefinitionSummary(resolver, summary, "level.2");

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Levels] Definitions resolved {summary}reason='{ReasonResolveDefinitions}'.",
                ColorOk);
        }

        [ContextMenu("QA/Levels/Select/Initial")]
        private void Qa_SelectInitial()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var sessionService = ResolveGlobal<ILevelSessionService>("ILevelSessionService");
            if (sessionService == null)
            {
                return;
            }

            sessionService.SelectInitial(ReasonSelectInitial);
        }

        [ContextMenu("QA/Levels/Select/Next")]
        private void Qa_SelectNext()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var sessionService = ResolveGlobal<ILevelSessionService>("ILevelSessionService");
            if (sessionService == null)
            {
                return;
            }

            sessionService.SelectNext(ReasonSelectNext);
        }

        [ContextMenu("QA/Levels/Select/Prev")]
        private void Qa_SelectPrev()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var sessionService = ResolveGlobal<ILevelSessionService>("ILevelSessionService");
            if (sessionService == null)
            {
                return;
            }

            sessionService.SelectPrevious(ReasonSelectPrev);
        }

        [ContextMenu("QA/Levels/ApplySelected")]
        private void Qa_ApplySelected()
        {
            if (!EnsureGateEnabled())
            {
                return;
            }

            var sessionService = ResolveGlobal<ILevelSessionService>("ILevelSessionService");
            if (sessionService == null)
            {
                return;
            }

            sessionService.ApplySelected(ReasonApplySelected);
        }

        [ContextMenu("QA/Levels/L01-GoToLevel (InPlace)")]
        private void Qa_GoToLevel1()
        {
            GoToLevelById("level.1", ReasonGoToLevel1);
        }

        [ContextMenu("QA/Levels/L02-GoToLevel (InPlace)")]
        private void Qa_GoToLevel2()
        {
            GoToLevelById("level.2", ReasonGoToLevel2);
        }

        private static void AppendDefinitionSummary(
            _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers.ILevelCatalogResolver resolver,
            StringBuilder summary,
            string levelId)
        {
            if (resolver.TryResolveDefinition(levelId, out var definition))
            {
                summary.Append($"levelId='{definition.LevelId}' contentId='{definition.ContentId}' ");
            }
            else
            {
                summary.Append($"levelId='{levelId}' contentId='<missing>' ");
            }
        }

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

            if (!DependencyManager.Provider.TryGetGlobal<PromotionGateService>(out var gate) || gate == null)
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

            var sessionService = ResolveGlobal<ILevelSessionService>("ILevelSessionService");
            if (sessionService != null)
            {
                if (sessionService.SelectLevelById(levelId, reason))
                {
                    sessionService.ApplySelected(reason);
                    return;
                }
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

            var levelManager = ResolveGlobal<ILevelManager>("ILevelManager");
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
