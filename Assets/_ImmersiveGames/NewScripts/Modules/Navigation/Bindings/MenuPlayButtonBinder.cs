using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
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
        [Header("LevelFlow")]
        [SerializeField]
        [Tooltip("LevelId canônico para iniciar gameplay via trilho oficial StartGameplayAsync(levelId).")]
        private string startLevelId = "level.1";

        private ILevelFlowRuntimeService _levelFlow;

        protected override void Awake()
        {
            base.Awake();

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

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[OBS][LevelFlow] MenuPlay -> StartGameplayAsync levelId='{LevelId.Normalize(startLevelId)}' reason='{actionReason}'.",
                DebugUtility.Colors.Info);

            // Fire-and-forget com captura de falhas.
            NavigationTaskRunner.FireAndForget(
                _levelFlow.StartGameplayAsync(startLevelId, actionReason),
                typeof(MenuPlayButtonBinder),
                $"Menu/Play levelId='{LevelId.Normalize(startLevelId)}'");

            return true;
        }
    }
}
