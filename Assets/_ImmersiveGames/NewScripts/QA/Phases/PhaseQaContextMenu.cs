// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaContextMenu.cs
// QA de ContentSwap (Phase) legado: ações objetivas para gerar evidência.
// Comentários PT; código EN.

#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Phases
{
    public sealed class PhaseQaContextMenu : MonoBehaviour
    {
        // Paleta do projeto
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        [Header("Default PhaseIds")]
        [SerializeField] private string inPlacePhaseId = "phase.2";
        [SerializeField] private string withTransitionPhaseId = "phase.2";

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
        private const string RequestedBy = "QA/ContentSwap/PhaseQaContextMenu";

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
            _ = RunG02WithTransitionAsync(withTransitionPhaseId, ReasonG02);
        }

        [ContextMenu("QA/ContentSwap/Dump - PhaseContext Snapshot")]
        private void Qa_Dump_PhaseContext()
        {
            DumpPhaseContext();
        }

        // ----------------------------
        // Editor convenience (MenuItem)
        // ----------------------------
#if UNITY_EDITOR
        [MenuItem("ImmersiveGames/NewScripts/QA/SceneFlow/ContentSwap/Select QA_ContentSwap Object", priority = 1190)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_ContentSwap");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu), "[QA][ContentSwap] QA_ContentSwap não encontrado no Hierarchy (Play Mode).", ColorWarn);
            }
        }

        // Mantém o comando visível apenas quando o projeto está em Play Mode.
        [MenuItem("ImmersiveGames/NewScripts/QA/SceneFlow/ContentSwap/Select QA_ContentSwap Object", true, 1190)]
        private static bool ValidateSelectQaObject()
        {
            return Application.isPlaying;
        }
#endif

        // ----------------------------
        // Implementations
        // ----------------------------

        private async Task RunG01InPlaceAsync(bool useFadeOpt, bool useLoadingHudOpt, string reason)
        {
            var svc = ResolveGlobal<IPhaseChangeService>();
            if (svc == null)
            {
                return;
            }

            var phaseId = string.IsNullOrWhiteSpace(inPlacePhaseId) ? "phase.2" : inPlacePhaseId.Trim();

            DebugUtility.Log(typeof(PhaseQaContextMenu),
                $"[QA][ContentSwap] G01 start phaseId='{phaseId}' reason='{reason}' (fade={useFadeOpt}, hud={useLoadingHudOpt}).",
                ColorInfo);

            try
            {
                var options = new PhaseChangeOptions
                {
                    UseFade = useFadeOpt,
                    UseLoadingHud = useLoadingHudOpt,
                    TimeoutMs = 20000
                };

                await svc.RequestPhaseInPlaceAsync(phaseId, reason, options);

                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][ContentSwap] G01 done phaseId='{phaseId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][ContentSwap] G01 failed phaseId='{phaseId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunG02WithTransitionAsync(string phaseIdRaw, string reason)
        {
            var phaseSvc = ResolveGlobal<IPhaseChangeService>();
            if (phaseSvc == null)
            {
                return;
            }

            var phaseId = string.IsNullOrWhiteSpace(phaseIdRaw) ? "phase.2" : phaseIdRaw.Trim();

            DebugUtility.Log(typeof(PhaseQaContextMenu),
                $"[QA][ContentSwap] G02 start phaseId='{phaseId}' reason='{reason}'.",
                ColorInfo);

            try
            {
                var request = BuildTransitionRequest();

                // IMPORTANTE:
                // - SingleClick deve chamar APENAS o PhaseChangeService.
                // - O PhaseChangeService já registra intent e dispara SceneFlow internamente.
                await phaseSvc.RequestPhaseWithTransitionAsync(phaseId, request, reason);

                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][ContentSwap] G02 done phaseId='{phaseId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][ContentSwap] G02 failed phaseId='{phaseId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private SceneTransitionRequest BuildTransitionRequest()
        {
            // Construção baseada no contrato existente do projeto (SceneFlowProfileId + request imutável).
            var pid = new SceneFlowProfileId(profileId);

            // Observação:
            // - contextSignature null: PhaseChangeService pode preencher/normalizar.
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

        private void DumpPhaseContext()
        {
            var ctx = ResolveGlobal<IPhaseContextService>();
            if (ctx == null)
            {
                return;
            }

            var current = ctx.Current;
            var pending = ctx.Pending;

            DebugUtility.Log(typeof(PhaseQaContextMenu),
                $"[QA][ContentSwap] PhaseContext snapshot current='{current.PhaseId}' pending='{pending.PhaseId}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>() where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    "[QA][ContentSwap] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][ContentSwap] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
