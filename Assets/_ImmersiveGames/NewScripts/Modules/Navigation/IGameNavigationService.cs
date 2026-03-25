using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Serviço de navegação em produção (NewScripts).
    /// Mantém rotas/planos centralizados e dispara transições via SceneFlow.
    /// </summary>
    public interface IGameNavigationService
    {
        /// <summary>
        /// Navega explicitamente para o Menu principal.
        /// </summary>
        Task GoToMenuAsync(string reason = null);

        /// <summary>
        /// Reinicia o gameplay atual usando o snapshot canônico de runtime.
        /// </summary>
        Task RestartAsync(string reason = null);

        /// <summary>
        /// Sai do gameplay para Menu (semântica de pós-jogo/pause).
        /// </summary>
        Task ExitToMenuAsync(string reason = null);

        /// <summary>
        /// ResolvePlayerActor a gameplay route principal a partir do catálogo core canônico.
        /// </summary>
        SceneRouteId ResolveGameplayRouteIdOrFail();

        /// <summary>
        /// Inicia gameplay a partir de um SceneRouteId já resolvido pelo LevelFlow.
        /// </summary>
        Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null);

        /// <summary>
        /// Navegação por intent core tipado (slots explícitos no catálogo).
        /// </summary>
        Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);
    }
}
