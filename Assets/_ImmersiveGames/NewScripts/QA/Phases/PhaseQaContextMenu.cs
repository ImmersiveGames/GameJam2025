// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaContextMenu.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.QA.Phases
{
    /// <summary>
    /// QA objetivo para evidência do ADR-0016:
    /// - In-Place (sem visuais por padrão): RequestPhaseInPlaceAsync(...)
    /// - WithTransition (SceneFlow profile=gameplay): RequestPhaseWithTransitionAsync(...)
    ///
    /// Observações:
    /// - NÃO assume nomes de cenas além da ActiveScene atual (fallback seguro).
    /// - NÃO força reload da ActiveScene (pode falhar se não houver cena temporária ativa).
    /// - Mantém ações mínimas para gerar evidências, evitando QA "genérico".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhaseQaContextMenu : MonoBehaviour
    {
        [Header("Phase QA")]
        [SerializeField] private string phaseId = "phase.2";
        [SerializeField] private string inPlaceReason = "QA/Phases/InPlace/NoVisuals";

        [Header("WithTransition QA (SceneFlow)")]
        [SerializeField] private string withTransitionReason = "QA/Phases/WithTransition/Gameplay";
        [SerializeField] private bool withTransitionUseFade = true;

        // Mantemos default gameplay para disparar WorldLifecycle reset via ScenesReady.
        [SerializeField] private string withTransitionProfileName = "gameplay";

        // Se vazio, usa a ActiveScene atual.
        [SerializeField] private string targetActiveSceneOverride = "";

        // Por padrão VAZIO (transição "minimal safe"): ainda gera eventos e reset.
        [SerializeField] private List<string> scenesToLoad = new();
        [SerializeField] private List<string> scenesToUnload = new();

        [ContextMenu("QA/Phases/InPlace/Commit (NoVisuals) (TC: ADR-0016/InPlace)")]
        private async void QA_Phase_InPlace_Commit_NoVisuals()
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                DebugUtility.LogWarning<PhaseQaContextMenu>(
                    "[QA][Phase] IPhaseChangeService não encontrado no DI global. Ação ignorada.");
                return;
            }

            var effectivePhaseId = NormalizeId(phaseId, fallback: "phase.2");
            var reason = NormalizeReason(inPlaceReason, fallback: "QA/Phases/InPlace/NoVisuals");

            DebugUtility.Log<PhaseQaContextMenu>(
                $"[QA][Phase] TC-ADR0016-INPLACE start phaseId='{effectivePhaseId}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            try
            {
                // options=null => Default (UseFade=false, UseLoadingHud=false, Timeout default)
                await service.RequestPhaseInPlaceAsync(effectivePhaseId, reason, options: null);

                DebugUtility.Log<PhaseQaContextMenu>(
                    $"[QA][Phase] TC-ADR0016-INPLACE done phaseId='{effectivePhaseId}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseQaContextMenu>(
                    $"[QA][Phase] TC-ADR0016-INPLACE failed ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        [ContextMenu("QA/Phases/WithTransition/Commit (Gameplay Minimal) (TC: ADR-0016/WithTransition)")]
        private async void QA_Phase_WithTransition_Commit_GameplayMinimal()
        {
            var service = ResolvePhaseChangeService();
            if (service == null)
            {
                DebugUtility.LogWarning<PhaseQaContextMenu>(
                    "[QA][Phase] IPhaseChangeService não encontrado no DI global. Ação ignorada.");
                return;
            }

            var effectivePhaseId = NormalizeId(phaseId, fallback: "phase.2");
            var reason = NormalizeReason(withTransitionReason, fallback: "QA/Phases/WithTransition/Gameplay");

            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;
            string targetActive = !string.IsNullOrWhiteSpace(targetActiveSceneOverride)
                ? targetActiveSceneOverride.Trim()
                : activeScene;

            var profileId = SceneFlowProfileId.FromName(NormalizeId(withTransitionProfileName, fallback: "gameplay"));

            // IMPORTANTE:
            // - Por default, usamos listas vazias => SceneFlow ainda emite Started/ScenesReady/Completed.
            // - WorldLifecycle reset ocorre se profile=gameplay (driver observa ScenesReady).
            // - Evita reload da ActiveScene (que pode requerer uma cena temporária ativa).
            var request = new SceneTransitionRequest(
                scenesToLoad: NormalizeSceneList(scenesToLoad),
                scenesToUnload: NormalizeSceneList(scenesToUnload),
                targetActiveScene: targetActive,
                useFade: withTransitionUseFade,
                transitionProfileId: profileId,
                contextSignature: null // deixa o pipeline computar uma assinatura estável baseada nos campos.
            );

            DebugUtility.Log<PhaseQaContextMenu>(
                $"[QA][Phase] TC-ADR0016-TRANSITION start phaseId='{effectivePhaseId}' reason='{reason}' " +
                $"profile='{profileId.Value}' active='{activeScene}' target='{targetActive}' " +
                $"loadCount='{request.ScenesToLoad.Count}' unloadCount='{request.ScenesToUnload.Count}' fade='{request.UseFade}'.",
                DebugUtility.Colors.Info);

            try
            {
                await service.RequestPhaseWithTransitionAsync(effectivePhaseId, request, reason, options: null);

                DebugUtility.Log<PhaseQaContextMenu>(
                    $"[QA][Phase] TC-ADR0016-TRANSITION requested phaseId='{effectivePhaseId}'. " +
                    "Commit ocorrerá via intent/bridge após ResetCompleted.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseQaContextMenu>(
                    $"[QA][Phase] TC-ADR0016-TRANSITION failed ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static IPhaseChangeService? ResolvePhaseChangeService()
        {
            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IPhaseChangeService>(out var service)
                ? service
                : null;
        }

        private static string NormalizeId(string? value, string fallback)
        {
            var s = value ?? string.Empty;
            s = s.Trim();
            return s.Length == 0 ? fallback : s;
        }

        private static string NormalizeReason(string? value, string fallback)
        {
            var s = value ?? string.Empty;
            s = s.Replace("\n", " ").Replace("\r", " ").Trim();
            return s.Length == 0 ? fallback : s;
        }

        private static IReadOnlyList<string> NormalizeSceneList(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var entry = (list[i] ?? string.Empty).Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                result.Add(entry);
            }

            return result.Count == 0 ? Array.Empty<string>() : result;
        }
    }
}
