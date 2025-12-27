/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Input;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Pause
{
    /// <summary>
    /// Controlador do overlay de pausa no UIGlobal.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseOverlayController : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private string showReason = "PauseOverlay/Show";
        [SerializeField] private string hideReason = "PauseOverlay/Hide";

        [Inject] private IInputModeService _inputModeService;
        [Inject] private IGameNavigationService _navigationService;

        private bool _dependenciesInjected;

        private void Start()
        {
            EnsureDependenciesInjected();

            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado no Inspector.");
            }

            if (_navigationService == null)
            {
                DebugUtility.LogVerbose(typeof(PauseOverlayController),
                    "[PauseOverlay] IGameNavigationService indisponivel (previsto para integracao futura).",
                    DebugUtility.Colors.Info);
            }
        }

        public void Show()
        {
            EnsureDependenciesInjected();

            if (!TrySetOverlayActive(true))
            {
                return;
            }

            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] Show -> GamePauseCommandEvent publicado.",
                DebugUtility.Colors.Info);

            if (_inputModeService != null)
            {
                _inputModeService.SetPauseOverlay(showReason);
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }
        }

        public void Hide()
        {
            EnsureDependenciesInjected();

            if (!TrySetOverlayActive(false))
            {
                return;
            }

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] Hide -> GameResumeRequestedEvent publicado.",
                DebugUtility.Colors.Info);

            if (_inputModeService != null)
            {
                _inputModeService.SetGameplay(hideReason);
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }
        }

        public void Resume()
        {
            Hide();
        }

        public void Toggle()
        {
            EnsureDependenciesInjected();

            bool isActive = overlayRoot != null && overlayRoot.activeSelf;
            if (isActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
        }

        private bool TrySetOverlayActive(bool active)
        {
            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado. Operacao ignorada.");
                return false;
            }

            overlayRoot.SetActive(active);
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] Overlay {(active ? "ativado" : "desativado")}.",
                DebugUtility.Colors.Info);
            return true;
        }
    }
}
