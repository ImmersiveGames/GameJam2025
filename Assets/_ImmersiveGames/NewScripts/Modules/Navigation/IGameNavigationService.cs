using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Serviço de navegação de produção (NewScripts).
    /// Mantém rotas/planos centralizados e dispara transições via SceneFlow.
    /// </summary>
    public interface IGameNavigationService
    {
        /// <summary>
        /// Navega explicitamente para o Menu principal.
        /// </summary>
        Task GoToMenuAsync(string reason = null);

        /// <summary>
        /// Reinicia o gameplay atual usando o intent canônico de gameplay.
        /// </summary>
        Task RestartAsync(string reason = null);

        /// <summary>
        /// Sai do gameplay para Menu (semântica de pós-jogo/pause).
        /// </summary>
        Task ExitToMenuAsync(string reason = null);

        /// <summary>
        /// Inicia gameplay a partir de um LevelId (LEGACY).
        /// Preferir ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct) ou StartGameplayRouteAsync(routeId,...).
        /// </summary>
        [System.Obsolete("Legacy API blocked. Use ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct) ou IGameNavigationService.StartGameplayRouteAsync(routeId, payload, reason).") ]
        Task StartGameplayAsync(LevelId levelId, string reason = null);


        /// <summary>
        /// Inicia gameplay a partir de um SceneRouteId já resolvido pelo LevelFlow.
        /// </summary>
        Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null);


        /// <summary>
        /// Navegação por intent core tipado (slots explícitos no catálogo).
        /// </summary>
        Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);

        /// <summary>
        /// Compatibilidade temporária: navegar por id de intent/rota (inclui extras/custom intents).
        /// </summary>
        [System.Obsolete("Prefira NavigateAsync(GameNavigationIntentKind, reason) para core intents; mantenha string para extras/custom.")]
        Task NavigateAsync(string routeId, string reason = null);

        /// <summary>
        /// Compatibilidade temporária para menu.
        /// </summary>
        [System.Obsolete("Use GoToMenuAsync(reason).")]
        Task RequestMenuAsync(string reason = null);

        /// <summary>
        /// Compatibilidade temporária para gameplay sem LevelId explícito.
        /// </summary>
        [System.Obsolete("Legacy API blocked. Use RestartAsync(reason) ou ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct).")]
        Task RequestGameplayAsync(string reason = null);
    }
}

