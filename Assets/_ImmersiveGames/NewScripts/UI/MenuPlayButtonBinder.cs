using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder de UI (produção) para o botão "Play" do Menu.
    /// Chama IGameNavigationService.RequestToGameplay().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string reason = "Menu/PlayButton";

        private IGameNavigationService _navigation;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            DependencyManager.Provider.TryGetGlobal(out _navigation);

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
            else
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Button não atribuído e GetComponent<Button>() falhou.");
            }

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] IGameNavigationService indisponível no Awake. Verifique se o GlobalBootstrap registrou antes do Menu.");
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }

        // Associe este método no Button.OnClick()
        public void OnClick()
        {
            if (_navigation == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _navigation);
            }

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Clique ignorado: IGameNavigationService indisponível.");
                return;
            }

            _ = _navigation.RequestToGameplay(reason);
        }
    }
}
