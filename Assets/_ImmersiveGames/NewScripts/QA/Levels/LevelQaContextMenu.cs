// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaContextMenu.cs
// QA de LevelManager: ações objetivas para evidência.

#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Levels;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
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

        private const string ReasonL01 = "QA/Levels/InPlace/DefaultIntroStage";
        private const string ReasonResolve = "QA/Levels/Resolve/Definitions";

        [Header("Fallback L01 Plan")]
        [SerializeField] private string fallbackLevelId = "level.1";
        [SerializeField] private string fallbackContentId = "content.2";
        [SerializeField] private string fallbackContentSignature = "content:level.1";

        [Header("Resolver")]
        [SerializeField] private bool preferCatalogResolution = true;

        [ContextMenu("QA/Levels/L01-GoToLevel (InPlace + IntroStage)")]
        private void Qa_L01_GoToLevel_InPlace()
        {
            _ = RunL01Async();
        }

        [ContextMenu("QA/Levels/Resolve/Definitions")]
        private void Qa_Resolve_Definitions()
        {
            RunResolveDefinitions();
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
                DebugUtility.Log(typeof(LevelQaContextMenu), "[QA][Level] QA_Level não encontrado no Hierarchy (Play Mode).", ColorWarn);
            }
        }
#endif

        private async Task RunL01Async()
        {
            var manager = ResolveGlobal<ILevelManager>("ILevelManager");
            if (manager == null)
            {
                return;
            }

            var reason = ReasonL01;
            var resolved = TryResolvePlan(fallbackLevelId, out var plan, out var options, out var source);
            if (!resolved)
            {
                plan = new LevelPlan(fallbackLevelId, fallbackContentId, fallbackContentSignature);
                options = LevelChangeOptions.Default.Clone();
                source = "Fallback";
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] L01 start levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode=InPlace reason='{reason}' contentSig='{plan.ContentSignature}' source='{source}'.",
                ColorInfo);

            try
            {
                await manager.RequestLevelInPlaceAsync(plan, reason, options);

                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L01 done levelId='{plan.LevelId}' contentId='{plan.ContentId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L01 failed levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private void RunResolveDefinitions()
        {
            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] Resolve Definitions start reason='{ReasonResolve}'.",
                ColorInfo);

            var catalogProvider = ResolveGlobal<ILevelCatalogProvider>("ILevelCatalogProvider");
            var definitionProvider = ResolveGlobal<ILevelDefinitionProvider>("ILevelDefinitionProvider");
            var resolver = ResolveGlobal<ILevelCatalogResolver>("ILevelCatalogResolver");

            var catalog = catalogProvider?.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][Level] LevelCatalog não encontrado; resolução abortada.",
                    ColorWarn);
                return;
            }

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] Catalog summary initial='{catalog.InitialLevelId}' orderedCount='{catalog.OrderedLevels.Count}' definitionsCount='{catalog.Definitions.Count}'.",
                ColorInfo);

            foreach (var levelId in catalog.OrderedLevels)
            {
                if (string.IsNullOrWhiteSpace(levelId))
                {
                    continue;
                }

                var resolved = definitionProvider != null && definitionProvider.TryGetDefinition(levelId, out var definition);
                var contentId = resolved ? definition.ContentId : "<missing>";
                var signature = resolved ? definition.ContentSignature : "<missing>";

                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] Catalog entry levelId='{levelId}' resolved='{resolved}' contentId='{contentId}' contentSig='{signature}'.",
                    ColorInfo);
            }

            if (resolver != null && resolver.TryResolveInitialPlan(out var plan, out var options))
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] Initial plan resolved levelId='{plan.LevelId}' contentId='{plan.ContentId}' contentSig='{plan.ContentSignature}' options='{FormatOptions(options)}'.",
                    ColorInfo);
            }
        }

        private bool TryResolvePlan(string levelId, out LevelPlan plan, out LevelChangeOptions options, out string source)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();
            source = "None";

            if (!preferCatalogResolution)
            {
                return false;
            }

            var resolver = ResolveGlobal<ILevelCatalogResolver>("ILevelCatalogResolver");
            if (resolver == null)
            {
                return false;
            }

            if (!resolver.TryResolvePlan(levelId, out plan, out options))
            {
                return false;
            }

            source = "Catalog";
            return true;
        }

        private static string FormatOptions(LevelChangeOptions options)
        {
            if (options == null || options.ContentSwapOptions == null)
            {
                return "<default>";
            }

            var contentSwapOptions = options.ContentSwapOptions;
            return $"fade={contentSwapOptions.UseFade}, hud={contentSwapOptions.UseLoadingHud}, timeout={contentSwapOptions.TimeoutMs}";
        }

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][Level] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] Serviço global ausente: {label}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
