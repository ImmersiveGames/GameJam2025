/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */

using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Pause
{
    /// <summary>
    /// Controlador do overlay de pausa no UIGlobal.
    /// Publica eventos (pause/resume/exit) e altera InputMode; não toca o SimulationGate diretamente.
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

        private void OnDisable()
        {
            // Safety net: se o overlay sumir enquanto ativo, publica Resume para não deixar a pausa “presa”.
            // Não interfere com ownership de terceiros (bridge só libera o handle dela).
            if (overlayRoot == null)
                return;

            if (!overlayRoot.activeSelf)
                return;

            overlayRoot.SetActive(false);

            try
            {
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            }
            catch
            {
                // EventBus pode não estar disponível durante teardown; ignore.
            }

            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] OnDisable (safety) -> overlay desativado e GameResumeRequestedEvent publicado.",
                DebugUtility.Colors.Info);
        }

        public void Show()
        {
            EnsureDependenciesInjected();

            if (!TrySetOverlayActive(true))
                return;

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
                return;

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

        public void ReturnToMenuFrontend()
        {
            EnsureDependenciesInjected();

            // Fecha o overlay (idempotente).
            TrySetOverlayActive(false);

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent());
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] ReturnToMenuFrontend -> GameExitToMenuRequestedEvent publicado.",
                DebugUtility.Colors.Info);

            if (_inputModeService != null)
            {
                _inputModeService.SetFrontendMenu("PauseOverlay/ReturnToMenuFrontend");
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }

            if (_navigationService == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IGameNavigationService indisponivel. Nao foi possivel navegar para o menu.");
                return;
            }

            _ = _navigationService.RequestToMenu("PauseOverlay/ReturnToMenuFrontend");
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
                return;

            var provider = DependencyManager.Provider;
            if (provider == null)
                return;

            try
            {
                provider.InjectDependencies(this);
                _dependenciesInjected = true;
            }
            catch
            {
                // DI pode não estar pronto; tentaremos novamente em chamadas futuras.
                _dependenciesInjected = false;
            }
        }

        private bool TrySetOverlayActive(bool active)
        {
            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado. Operacao ignorada.");
                return false;
            }

            if (overlayRoot.activeSelf == active)
            {
                DebugUtility.LogVerbose(typeof(PauseOverlayController),
                    $"[PauseOverlay] Operacao ignorada: overlay já está {(active ? "ativo" : "inativo")}.",
                    DebugUtility.Colors.Info);
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
