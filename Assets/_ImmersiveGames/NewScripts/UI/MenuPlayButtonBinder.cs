using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder (produção) para o botão "Play" do Frontend.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    /// - Recomendação de produção: NÃO desabilitar o botão por tempo;
    ///   confiar no debounce/in-flight guard do GameNavigationService.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : FrontendButtonBinderBase
    {
        private IGameNavigationService _navigation;

        protected override void Awake()
        {
            base.Awake();

            // Tentativa early: se não estiver pronto ainda, tentamos de novo no clique.
            DependencyManager.Provider.TryGetGlobal(out _navigation);

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] IGameNavigationService indisponível no Awake. Verifique se o GlobalBootstrap registrou antes do Frontend.");
            }
        }

        protected override bool OnClickCore(string actionReason)
        {
            if (_navigation == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _navigation);
            }

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Clique ignorado: IGameNavigationService indisponível.");
                // Se a base estiver configurada para desabilitar durante ação, isso garante reabilitar.
                return false;
            }

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[Navigation] Play solicitado. reason='{actionReason}'.",
                DebugUtility.Colors.Info);

            // Fire-and-forget: o serviço deve fazer guard de in-flight/debounce.
            _ = _navigation.RequestGameplayAsync(actionReason);

            return true;
        }
    }
}
