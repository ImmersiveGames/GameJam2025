#if UNITY_EDITOR
#nullable enable

using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Bindings
{
    public sealed class ContentSwapDevContextMenu : MonoBehaviour
    {
        // Paleta do projeto
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        [Header("Default ContentIds")]
        [SerializeField] private string inPlaceContentId = "content.2";

        // Gate default (Baseline 2.2): ContentSwap sem visuais e sem IntroStageController.
        private const string ReasonG01 = "QA/ContentSwap/InPlace/NoVisuals";


        // ----------------------------
        // Public QA Actions (ContextMenu)
        // ----------------------------

        [ContextMenu("QA/ContentSwap/G01 - InPlace (NoVisuals)")]
        private void Qa_G01_InPlace_NoVisuals()
        {
            _ = RunG01InPlaceAsync(useFadeOpt: false, useLoadingHudOpt: false, reason: ReasonG01);
        }

        [ContextMenu("QA/ContentSwap/Dump - ContentSwapContext Snapshot")]
        private void Qa_Dump_ContentSwapContext()
        {
            DumpContentSwapContext();
        }

        // ----------------------------
        // Editor convenience (MenuItem)
        // ----------------------------
#if UNITY_EDITOR
        [MenuItem("Tools/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 10)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_ContentSwap");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(ContentSwapDevContextMenu), "[QA][ContentSwap] QA_ContentSwap não encontrado no Hierarchy (Play Mode).", ColorWarn);
            }
        }
#endif

        // ----------------------------
        // Implementations
        // ----------------------------

        private async Task RunG01InPlaceAsync(bool useFadeOpt, bool useLoadingHudOpt, string reason)
        {
            var svc = ResolveGlobal<IContentSwapChangeService>();
            if (svc == null)
            {
                return;
            }

            string contentId = string.IsNullOrWhiteSpace(inPlaceContentId) ? "content.2" : inPlaceContentId.Trim();

            DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                $"[QA][ContentSwap] G01 start contentId='{contentId}' reason='{reason}' (fade={useFadeOpt}, hud={useLoadingHudOpt}).",
                ColorInfo);

            try
            {
                var options = new ContentSwapOptions
                {
                    UseFade = useFadeOpt,
                    UseLoadingHud = useLoadingHudOpt,
                    TimeoutMs = 20000
                };

                await svc.RequestContentSwapInPlaceAsync(contentId, reason, options);

                DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                    $"[QA][ContentSwap] G01 done contentId='{contentId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                    $"[QA][ContentSwap] G01 failed contentId='{contentId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private void DumpContentSwapContext()
        {
            var ctx = ResolveGlobal<IContentSwapContextService>();
            if (ctx == null)
            {
                return;
            }

            var current = ctx.Current;
            var pending = ctx.Pending;

            DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                $"[QA][ContentSwap] ContentSwapContext snapshot current='{current.contentId}' pending='{pending.contentId}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>() where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                    "[QA][ContentSwap] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                    $"[QA][ContentSwap] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }

    }
}

#endif
