// Assets/_ImmersiveGames/NewScripts/QA/ContentSwap/ContentSwapQaContextMenu.cs
// QA de ContentSwap: ações objetivas para gerar evidência.
// Comentários PT; código EN.

#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.ContentSwap
{
    public sealed class ContentSwapQaContextMenu : MonoBehaviour
    {
        // Paleta do projeto
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        [Header("Default ContentIds")]
        [SerializeField] private string inPlaceContentId = "content.2";
        [SerializeField] private string withTransitionContentId = "content.2";

        [Header("WithTransition SceneFlow Request")]
        [SerializeField] private string profileId = "gameplay";
        [SerializeField] private string targetActiveScene = "GameplayScene";
        [SerializeField] private string[] scenesToLoad = { "GameplayScene", "UIGlobalScene" };
        [SerializeField] private string[] scenesToUnload = Array.Empty<string>();
        [SerializeField] private bool useFade = true;

        // Gate default (Baseline 2.2): ContentSwap sem visuais e sem IntroStage.
        private const string ReasonG01 = "QA/ContentSwap/InPlace/NoVisuals";
        private const string ReasonG02 = "QA/ContentSwap/WithTransition/SceneFlow";

        // Recomendado: rastreável no log/observability
        private const string RequestedBy = "QA/ContentSwap/ContentSwapQaContextMenu";

        // ----------------------------
        // Public QA Actions (ContextMenu)
        // ----------------------------

        [ContextMenu("QA/ContentSwap/G01 - InPlace (NoVisuals)")]
        private void Qa_G01_InPlace_NoVisuals()
        {
            _ = RunG01InPlaceAsync(useFadeOpt: false, useLoadingHudOpt: false, reason: ReasonG01);
        }

        [ContextMenu("QA/ContentSwap/G02 - WithTransition (SingleClick)")]
        private void Qa_G02_WithTransition_SingleClick()
        {
            _ = RunG02WithTransitionAsync(withTransitionContentId, ReasonG02);
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
                DebugUtility.Log(typeof(ContentSwapQaContextMenu), "[QA][ContentSwap] QA_ContentSwap não encontrado no Hierarchy (Play Mode).", ColorWarn);
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

            var contentId = string.IsNullOrWhiteSpace(inPlaceContentId) ? "content.2" : inPlaceContentId.Trim();

            DebugUtility.Log(typeof(ContentSwapQaContextMenu),
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

                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    $"[QA][ContentSwap] G01 done contentId='{contentId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    $"[QA][ContentSwap] G01 failed contentId='{contentId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunG02WithTransitionAsync(string contentIdRaw, string reason)
        {
            var contentSwapSvc = ResolveGlobal<IContentSwapChangeService>();
            if (contentSwapSvc == null)
            {
                return;
            }

            var contentId = string.IsNullOrWhiteSpace(contentIdRaw) ? "content.2" : contentIdRaw.Trim();

            DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                $"[QA][ContentSwap] G02 start contentId='{contentId}' reason='{reason}'.",
                ColorInfo);

            try
            {
                var request = BuildTransitionRequest();

                // IMPORTANTE:
                // - SingleClick deve chamar APENAS o ContentSwapChangeService.
                // - O ContentSwapChangeService já registra intent e dispara SceneFlow internamente.
                await contentSwapSvc.RequestContentSwapWithTransitionAsync(contentId, request, reason);

                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    $"[QA][ContentSwap] G02 done contentId='{contentId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    $"[QA][ContentSwap] G02 failed contentId='{contentId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private SceneTransitionRequest BuildTransitionRequest()
        {
            // Construção baseada no contrato existente do projeto (SceneFlowProfileId + request imutável).
            var pid = new SceneFlowProfileId(profileId);

            // Observação:
            // - contextSignature null: ContentSwapChangeService pode preencher/normalizar.
            // - requestedBy: rastreável para diagnósticos.
            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad ?? Array.Empty<string>(),
                scenesToUnload: scenesToUnload ?? Array.Empty<string>(),
                targetActiveScene: string.IsNullOrWhiteSpace(targetActiveScene) ? "GameplayScene" : targetActiveScene.Trim(),
                useFade: useFade,
                transitionProfileId: pid,
                contextSignature: null,
                requestedBy: RequestedBy
            );
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

            DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                $"[QA][ContentSwap] ContentSwapContext snapshot current='{current.ContentId}' pending='{pending.ContentId}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>() where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    "[QA][ContentSwap] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(ContentSwapQaContextMenu),
                    $"[QA][ContentSwap] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
