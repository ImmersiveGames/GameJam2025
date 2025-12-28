// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    /// <summary>
    /// Trigger simples de produção para validar navegação sem QA tools.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class GameNavigationDebugTrigger : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Inject] private IGameNavigationService _navigationService;

        private bool _dependenciesInjected;

        private void Start()
        {
            EnsureDependenciesInjected();

            if (_navigationService == null)
            {
                DebugUtility.LogWarning(typeof(GameNavigationDebugTrigger),
                    "[Navigation] IGameNavigationService não disponível no DI global. ContextMenus não funcionarão.");
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

        [ContextMenu("Debug/Navigation/Go To Gameplay")]
        public void GoToGameplay()
        {
            EnsureDependenciesInjected();

            if (_navigationService == null)
            {
                DebugUtility.LogError(typeof(GameNavigationDebugTrigger),
                    "[Navigation] IGameNavigationService indisponível. Não foi possível navegar para Gameplay.");
                return;
            }

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameNavigationDebugTrigger),
                    "[Navigation] ContextMenu -> Go To Gameplay.",
                    DebugUtility.Colors.Info);
            }

            _ = _navigationService.RequestToGameplay("ContextMenu/GoToGameplay");
        }

        [ContextMenu("Debug/Navigation/Go To Menu")]
        public void GoToMenu()
        {
            EnsureDependenciesInjected();

            if (_navigationService == null)
            {
                DebugUtility.LogError(typeof(GameNavigationDebugTrigger),
                    "[Navigation] IGameNavigationService indisponível. Não foi possível navegar para Menu.");
                return;
            }

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameNavigationDebugTrigger),
                    "[Navigation] ContextMenu -> Go To Menu.",
                    DebugUtility.Colors.Info);
            }

            _ = _navigationService.RequestToMenu("ContextMenu/GoToMenu");
        }
    }
}
#endif
