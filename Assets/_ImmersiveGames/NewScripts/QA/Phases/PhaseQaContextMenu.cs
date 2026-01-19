// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaContextMenu.cs
// QA de Phases (Baseline 2.2): ações objetivas para gerar evidência de completude.
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

        // Gate default (mantém a disciplina do ADR-0018: G-01 “sem visuais”)
        private const string ReasonG01 = "QA/Phases/InPlace/NoVisuals";
        private const string ReasonG03 = "QA/Phases/WithTransition/SceneFlow";

        // Recomendado: rastreável no log/observability
        private const string RequestedBy = "QA/Phases/PhaseQaContextMenu";

        // ----------------------------
        // Public QA Actions (ContextMenu)
        // ----------------------------

        [ContextMenu("QA/Phases/G01 - InPlace (NoVisuals)")]
        private void Qa_G01_InPlace_NoVisuals()
        {
            _ = RunG01InPlaceAsync(useFadeOpt: false, useLoadingHudOpt: false, reason: ReasonG01);
        }

        [ContextMenu("QA/Phases/DEV - InPlace (WithFade+HUD)")]
        private void Qa_DEV_InPlace_WithFade()
        {
            _ = RunG01InPlaceAsync(useFadeOpt: true, useLoadingHudOpt: true, reason: "QA/Phases/InPlace/DevVisuals");
        }

        [ContextMenu("QA/Phases/G03 - WithTransition (SingleClick)")]
        private void Qa_G03_WithTransition_SingleClick()
        {
            _ = RunG03WithTransitionAsync(withTransitionPhaseId, ReasonG03);
        }

        [ContextMenu("QA/Phases/Dump - PhaseContext Snapshot")]
        private void Qa_Dump_PhaseContext()
        {
            DumpPhaseContext();
        }

        // ----------------------------
        // Editor convenience (MenuItem)
        // ----------------------------
#if UNITY_EDITOR
        [MenuItem("Tools/NewScripts/QA/Phases/Select QA_Phase Object", priority = 10)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_Phase");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu), "[QA][Phase] QA_Phase não encontrado no Hierarchy (Play Mode).", ColorWarn);
            }
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
                $"[QA][Phase] TC-G01-INPLACE start phaseId='{phaseId}' reason='{reason}' (fade={useFadeOpt}, hud={useLoadingHudOpt}).",
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
                    $"[QA][Phase] TC-G01-INPLACE done phaseId='{phaseId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][Phase] TC-G01-INPLACE failed phaseId='{phaseId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunG03WithTransitionAsync(string phaseIdRaw, string reason)
        {
            var phaseSvc = ResolveGlobal<IPhaseChangeService>();
            if (phaseSvc == null)
            {
                return;
            }

            var phaseId = string.IsNullOrWhiteSpace(phaseIdRaw) ? "phase.2" : phaseIdRaw.Trim();

            DebugUtility.Log(typeof(PhaseQaContextMenu),
                $"[QA][Phase] TC-G03-WITHTRANSITION start phaseId='{phaseId}' reason='{reason}'.",
                ColorInfo);

            try
            {
                var request = BuildTransitionRequest();

                // IMPORTANTE:
                // - SingleClick deve chamar APENAS o PhaseChangeService.
                // - O PhaseChangeService já registra intent e dispara SceneFlow internamente.
                await phaseSvc.RequestPhaseWithTransitionAsync(phaseId, request, reason);

                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][Phase] TC-G03-WITHTRANSITION done phaseId='{phaseId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][Phase] TC-G03-WITHTRANSITION failed phaseId='{phaseId}' ex='{ex.GetType().Name}: {ex.Message}'.",
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
                $"[QA][Phase] PhaseContext snapshot current='{current.PhaseId}' pending='{pending.PhaseId}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>() where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    "[QA][Phase] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(PhaseQaContextMenu),
                    $"[QA][Phase] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
