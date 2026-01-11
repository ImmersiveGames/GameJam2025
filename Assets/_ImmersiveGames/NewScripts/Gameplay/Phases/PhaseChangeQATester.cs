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

        [ContextMenu("QA/Phase/InPlace -> Phase1")]
        private async void QA_InPlace_Phase1()
            => await RequestInPlaceAsync(Phase1Id, "QA/Phase/InPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase2")]
        private async void QA_InPlace_Phase2()
            => await RequestInPlaceAsync(Phase2Id, "QA/Phase/InPlace");

        [ContextMenu("QA/Phase/InPlace -> Phase3")]
        private async void QA_InPlace_Phase3()
            => await RequestInPlaceAsync(Phase3Id, "QA/Phase/InPlace");

        // --------------------------------
        // WithTransition (com SceneFlow)
        // --------------------------------

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

            var options = new PhaseChangeOptions
            {
                // ADR-0017: InPlace não deve executar Fade/Loading HUD/SceneFlow.
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
    }
}
