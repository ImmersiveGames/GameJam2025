#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases.QA
{
    /// <summary>
    /// QA helper para acionar troca de fase com e sem SceneFlow via Context Menu.
    /// Objetivo: validar fluxo InPlace (sem Fade/SceneFlow) e WithTransition (com SceneFlow).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhaseChangeQATester : MonoBehaviour
    {
        private const string Phase1Id = "Phase1";
        private const string Phase2Id = "Phase2";
        private const string Phase3Id = "Phase3";

        // -------------------------
        // InPlace (sem SceneFlow)
        // -------------------------

        [ContextMenu("QA/Phase/Advance In-Place (TestCase: PhaseInPlace)")]
        private async void QA_AdvanceInPlace_TestCase()
            => await RequestInPlaceAsync(Phase1Id, "QA/TestCase:PhaseInPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase1")]
        private async void QA_InPlace_Phase1()
            => await RequestInPlaceAsync(Phase1Id, "QA/Phase/InPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase2")]
        private async void QA_InPlace_Phase2()
            => await RequestInPlaceAsync(Phase2Id, "QA/Phase/InPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase3")]
        private async void QA_InPlace_Phase3()
            => await RequestInPlaceAsync(Phase3Id, "QA/Phase/InPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase2 (Expect Pregame + gate fechado até complete)")]
        private async void QA_InPlace_Phase2_ExpectPregame()
        {
            DebugUtility.Log<PhaseChangeQATester>(
                "[OBS][QA][Phase] InPlace -> Phase2 solicitado. Expectativa: Pregame + gate fechado até Complete.",
                DebugUtility.Colors.Info);
            await RequestInPlaceAsync(Phase2Id, "QA/Phase/InPlaceExpectPregame");
        }

        [ContextMenu("QA/Phase/Advance In-Place -> Expect Pregame (gate fechado)")]
        private async void QA_AdvanceInPlace_ExpectPregame()
        {
            DebugUtility.Log<PhaseChangeQATester>(
                "[OBS][QA][Phase] Advance In-Place solicitado. Expectativa: Pregame + gate fechado até Complete.",
                DebugUtility.Colors.Info);
            await RequestInPlaceAsync(Phase2Id, "QA/Phase/InPlaceExpectPregame");
        }

        [ContextMenu("QA/Phase/Restart Current Phase -> Expect Pregame (gate fechado)")]
        private async void QA_RestartCurrentPhase_ExpectPregame()
        {
            var phaseContext = ResolvePhaseContextService();
            if (phaseContext == null)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    "[QA][Phase] IPhaseContextService não encontrado; Restart Current Phase ignorado.");
                return;
            }

            var current = phaseContext.Current;
            if (!current.IsValid)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    "[QA][Phase] Phase atual inválida; usando fallback Phase1 para Restart.");
                current = new PhasePlan(Phase1Id, string.Empty);
            }

            DebugUtility.Log<PhaseChangeQATester>(
                $"[OBS][QA][Phase] Restart Current Phase solicitado. phaseId='{current.PhaseId}'. " +
                "Expectativa: Pregame + gate fechado até Complete.",
                DebugUtility.Colors.Info);

            await RequestInPlaceAsync(current.PhaseId, "QA/Phase/RestartCurrent");
        }

        [ContextMenu("QA/Phase/Restart Current Phase (Expect Pregame + gate fechado)")]
        private async void QA_RestartCurrentPhase_ExpectPregame_Label()
        {
            DebugUtility.Log<PhaseChangeQATester>(
                "[OBS][QA][Phase] Restart Current Phase solicitado (label alternativo). Expectativa: Pregame + gate fechado.",
                DebugUtility.Colors.Info);
            await RequestInPlaceAsync(ResolveCurrentPhaseId(), "QA/Phase/RestartCurrent");
        }

        // --------------------------------
        // WithTransition (com SceneFlow)
        // --------------------------------

        [ContextMenu("QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)")]
        private async void QA_AdvanceWithTransition_TestCase()
            => await RequestWithTransitionAsync(Phase1Id, "QA/TestCase:PhaseWithTransition");

        [ContextMenu("QA/Phase/WithTransition -> Phase1 (SceneFlow)")]
        private async void QA_WithTransition_Phase1()
            => await RequestWithTransitionAsync(Phase1Id, "QA/Phase/WithTransition");

        [ContextMenu("QA/Phase/WithTransition -> Phase2 (SceneFlow)")]
        private async void QA_WithTransition_Phase2()
            => await RequestWithTransitionAsync(Phase2Id, "QA/Phase/WithTransition");

        [ContextMenu("QA/Phase/WithTransition -> Phase3 (SceneFlow)")]
        private async void QA_WithTransition_Phase3()
            => await RequestWithTransitionAsync(Phase3Id, "QA/Phase/WithTransition");

        // -------------------------
        // Core helpers
        // -------------------------

        private static async System.Threading.Tasks.Task RequestInPlaceAsync(string phaseId, string reason)
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                return;
            }

            // ADR-0016: InPlace não deve executar Fade/Loading HUD/SceneFlow por padrão.
            // Este testcase força explicitamente os flags para não depender de defaults globais.
            var options = new PhaseChangeOptions
            {
                UseFade = false,
                UseLoadingHud = false,
                TimeoutMs = PhaseChangeOptions.DefaultTimeoutMs
            };

            DebugUtility.Log<PhaseChangeQATester>(
                $"[QA][Phase] Solicitando Change InPlace -> '{phaseId}'. reason='{reason}'.");

            try
            {
                await service.RequestPhaseInPlaceAsync(phaseId, reason, options);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    $"[QA][Phase] Exception ao solicitar Change InPlace. phaseId='{phaseId}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static async System.Threading.Tasks.Task RequestWithTransitionAsync(string phaseId, string reason)
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                return;
            }

            var transition = BuildGameplayReloadRequest();
            if (transition == null)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    "[QA][Phase] Não foi possível resolver SceneTransitionRequest para QA.");
                return;
            }

            var options = new PhaseChangeOptions
            {
                TimeoutMs = PhaseChangeOptions.DefaultTimeoutMs
            };

            DebugUtility.Log<PhaseChangeQATester>(
                $"[QA][Phase] Solicitando Change WithTransition -> '{phaseId}' (SceneFlow). reason='{reason}'.");

            try
            {
                await service.RequestPhaseWithTransitionAsync(phaseId, transition, reason, options);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    $"[QA][Phase] Exception ao solicitar Change WithTransition. phaseId='{phaseId}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static SceneTransitionRequest? BuildGameplayReloadRequest()
        {
            var activeScene = SceneManager.GetActiveScene().name;
            var catalog = GameNavigationCatalog.CreateDefaultMinimal();

            if (string.Equals(activeScene, GameNavigationCatalog.SceneGameplay, StringComparison.Ordinal))
            {
                return catalog.BuildGameplayReload();
            }

            DebugUtility.LogWarning<PhaseChangeQATester>(
                $"[QA][Phase] Cena ativa '{activeScene}' não é Gameplay. QA WithTransition ignorado.");
            return null;
        }

        private static IPhaseChangeService? ResolvePhaseChangeService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseChangeService>(out var service) || service == null)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    "[QA][Phase] IPhaseChangeService não encontrado no DI global.");
                return null;
            }

            return service;
        }

        private static IPhaseContextService? ResolvePhaseContextService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseContextService>(out var service) || service == null)
            {
                return null;
            }

            return service;
        }

        private static string ResolveCurrentPhaseId()
        {
            var phaseContext = ResolvePhaseContextService();
            if (phaseContext == null || !phaseContext.Current.IsValid)
            {
                return Phase1Id;
            }

            return phaseContext.Current.PhaseId;
        }
    }
}
