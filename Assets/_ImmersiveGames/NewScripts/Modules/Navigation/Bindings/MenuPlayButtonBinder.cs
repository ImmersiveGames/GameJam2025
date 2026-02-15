using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bindings
{
    /// <summary>
    /// Binder (produção) para o botão "Play" do Frontend.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem coroutines.
    /// - Recomendação de produção: NÃO desabilitar o botão por tempo;
    ///   confiar no debounce/in-flight guard do GameNavigationService.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : FrontendButtonBinderBase
    {
        private ILevelFlowRuntimeService _levelFlow;
        private LevelId _startLevelId;

        protected override void Awake()
        {
            base.Awake();

            ResolveConfiguredStartLevelOrFail();

            // Tentativa early: se não estiver pronto ainda, tentamos de novo no clique.
            DependencyManager.Provider.TryGetGlobal(out _levelFlow);

            if (_levelFlow == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[LevelFlow] ILevelFlowRuntimeService indisponível no Awake. Verifique se o GlobalCompositionRoot registrou antes do Frontend.");
            }
        }

        protected override bool OnClickCore(string actionReason)
        {
            if (_levelFlow == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _levelFlow);
            }

            if (_levelFlow == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[LevelFlow] Clique ignorado: ILevelFlowRuntimeService indisponível.");
                // Se a base estiver configurada para desabilitar durante ação, isso garante reabilitar.
                return false;
            }

            string normalizedLevelId = _startLevelId.Value;
            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[OBS][LevelFlow] MenuPlay -> StartGameplayAsync levelId='{normalizedLevelId}' reason='{actionReason}'.",
                DebugUtility.Colors.Info);

            // Fire-and-forget com captura de falhas.
            NavigationTaskRunner.FireAndForget(
                _levelFlow.StartGameplayAsync(normalizedLevelId, actionReason),
                typeof(MenuPlayButtonBinder),
                $"Menu/Play levelId='{normalizedLevelId}'");

            return true;
        }

        private void ResolveConfiguredStartLevelOrFail()
        {
            if (DependencyManager.Provider == null)
            {
                FailFastConfig("DependencyManager.Provider está null ao resolver start level do MenuPlayButtonBinder.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var bootstrapConfig) || bootstrapConfig == null)
            {
                FailFastConfig("NewScriptsBootstrapConfigAsset indisponível no escopo global para resolver start level do MenuPlayButtonBinder.");
            }

            _startLevelId = bootstrapConfig.StartGameplayLevelId;
            if (!_startLevelId.IsValid)
            {
                FailFastConfig($"NewScriptsBootstrapConfigAsset.StartGameplayLevelId inválido. asset='{bootstrapConfig.name}', value='{_startLevelId}'.");
            }
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<MenuPlayButtonBinder>(message);
            throw new InvalidOperationException(message);
        }
    }
}
