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
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhaseChangeQATester : MonoBehaviour
    {
        private const string Phase2Id = "Phase2";

        [ContextMenu("QA/Phase/Change InPlace -> Phase2")]
        private async void QA_ChangeInPlacePhase2()
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                return;
            }

            var options = new PhaseChangeOptions
            {
                UseFade = true,
                UseLoadingHud = true,
                TimeoutMs = PhaseChangeOptions.DefaultTimeoutMs
            };

            DebugUtility.Log<PhaseChangeQATester>(
                "[QA][Phase] Solicitando Change InPlace -> Phase2.");

            try
            {
                await service.RequestPhaseInPlaceAsync(Phase2Id, "QA/Phase/InPlace", options);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    $"[QA][Phase] Exception ao solicitar Change InPlace. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        [ContextMenu("QA/Phase/Change WithTransition -> Phase2 (SceneFlow)")]
        private async void QA_ChangeWithTransitionPhase2()
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                return;
            }

            var transition = BuildTransitionRequest();
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
                "[QA][Phase] Solicitando Change WithTransition -> Phase2 (SceneFlow).");

            try
            {
                await service.RequestPhaseWithTransitionAsync(Phase2Id, transition, "QA/Phase/WithTransition", options);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseChangeQATester>(
                    $"[QA][Phase] Exception ao solicitar Change WithTransition. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static SceneTransitionRequest? BuildTransitionRequest()
        {
            var activeScene = SceneManager.GetActiveScene().name;
            var catalog = GameNavigationCatalog.CreateDefaultMinimal();

            if (string.Equals(activeScene, GameNavigationCatalog.SceneMenu, StringComparison.Ordinal))
            {
                return catalog.BuildMenuToGameplay();
            }

            if (string.Equals(activeScene, GameNavigationCatalog.SceneGameplay, StringComparison.Ordinal))
            {
                return catalog.BuildGameplayToMenu();
            }

            DebugUtility.LogWarning<PhaseChangeQATester>(
                $"[QA][Phase] Cena ativa '{activeScene}' não suportada no catálogo QA.");
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
