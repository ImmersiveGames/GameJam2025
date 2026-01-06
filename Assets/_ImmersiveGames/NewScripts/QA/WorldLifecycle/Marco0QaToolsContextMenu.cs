#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.WorldLifecycle
{
    /// <summary>
    /// Marco 0 — Ferramentas QA enxutas (ContextMenu no Inspector).
    ///
    /// Como usar:
    /// - Adicione este componente em um GameObject de uma cena de teste (ex.: NewBootstrap/WorldRoot)
    /// - Use o menu de contexto do componente no Inspector (três pontinhos)
    ///
    /// Objetivo:
    /// - Facilitar a validação do Marco 0 sem menus de Editor e sem UI runtime.
    /// </summary>
    public sealed class Marco0QaToolsContextMenu : MonoBehaviour
    {
        private const string RunKey = "NewScripts.QA.Marco0PhaseObservability.RunRequested";
        private const string LogPrefix = "[QA][Marco0]";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        // 1) Preparar o próximo Play para rodar o Runner (delimitador MARCO0_START/END)
        [ContextMenu("QA Marco0/Preparar: Rodar no próximo Play (marca o teste)")]
        private void Prepare_RunNextPlay()
        {
            PlayerPrefs.SetInt(RunKey, 1);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} Preparado. No próximo Play Mode o runner vai imprimir MARCO0_START/MARCO0_END e limpar a flag.");
        }

        // 2) Limpar a flag (não roda no próximo play)
        [ContextMenu("QA Marco0/Limpar: não rodar no próximo Play")]
        private void Clear_DoNotRunNextPlay()
        {
            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} Flag limpa. O runner NÃO vai rodar no próximo Play Mode.");
        }

        // 3) Mostrar status
        [ContextMenu("QA Marco0/Status: mostrar se está preparado")]
        private void Status_Print()
        {
            var armed = PlayerPrefs.GetInt(RunKey, 0) == 1;
            Debug.Log($"{LogPrefix} Status: {(armed ? "PREPARADO (vai rodar no próximo Play)" : "NÃO preparado")} | {RunKey}={(armed ? 1 : 0)}");
        }

        // 4) Forçar evidência agora (gera [OBS][Phase] ResetRequested imediatamente)
        [ContextMenu("QA Marco0/Agora: Forçar evidência (Reset)")]
        private void Now_ForceEvidence_Reset()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IWorldResetRequestService>(out var reset) || reset == null)
            {
                Debug.LogWarning($"{LogPrefix} Não foi possível resolver IWorldResetRequestService no DI global. (Você está em Play Mode? O bootstrap terminou?)");
                return;
            }

            _ = reset.RequestResetAsync("qa_marco0_reset");
            Debug.Log($"{LogPrefix} Reset solicitado. Procure no log: {WorldLifecyclePhaseObservabilitySignatures.ResetRequested} ...");
        }
        [ContextMenu("QA Marco0/Limpar: status da sessão (permitir rodar de novo no mesmo Play)")]
        private void ClearSession_RunAgainSamePlay()
        {
            PlayerPrefs.SetInt("NewScripts.QA.Marco0PhaseObservability.RanThisPlay", 0);
            PlayerPrefs.Save();
            Debug.Log("[QA][Marco0] SessionKey limpo. Se você armar RunKey e houver novo Bootstrap, pode rodar novamente.");
        }
    }
}
#endif
