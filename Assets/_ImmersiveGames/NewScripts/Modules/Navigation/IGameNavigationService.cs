using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Servico de navegacao de producao (NewScripts).
    /// Mantem rotas/planos centralizados e dispara transicoes via SceneFlow.
    /// </summary>
    public interface IGameNavigationService
    {
        /// <summary>
        /// Navega explicitamente para o Menu principal.
        /// </summary>
        Task GoToMenuAsync(string reason = null);

        /// <summary>
        /// Reinicia o gameplay atual usando o snapshot canonico de runtime.
        /// </summary>
        Task RestartAsync(string reason = null);

        /// <summary>
        /// Sai do gameplay para Menu (semantica de pos-jogo/pause).
        /// </summary>
        Task ExitToMenuAsync(string reason = null);

        /// <summary>
        /// Resolve a gameplay route principal a partir do catalogo core canonico.
        /// </summary>
        SceneRouteId ResolveGameplayRouteIdOrFail();

        /// <summary>
        /// Inicia gameplay a partir de um SceneRouteId ja resolvido pelo LevelFlow.
        /// </summary>
        Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null);

        /// <summary>
        /// Navegacao por intent core tipado (slots explicitos no catalogo).
        /// </summary>
        Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);
    }
}
