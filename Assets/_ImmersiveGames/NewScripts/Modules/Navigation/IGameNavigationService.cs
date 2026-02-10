using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
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
        /// Inicia gameplay a partir de um LevelId (LevelFlow -> SceneRouteId + payload).
        /// </summary>
        Task StartGameplayAsync(LevelId levelId, string reason = null);

        /// <summary>
        /// Compatibilidade temporária: navegar por id de intent/rota.
        /// </summary>
        [System.Obsolete("Use métodos explícitos: GoToMenuAsync, RestartAsync, ExitToMenuAsync ou StartGameplayAsync(levelId, reason).")]
        Task NavigateAsync(string routeId, string reason = null);

        /// <summary>
        /// Compatibilidade temporária para menu.
        /// </summary>
        [System.Obsolete("Use GoToMenuAsync(reason).")]
        Task RequestMenuAsync(string reason = null);

        /// <summary>
        /// Compatibilidade temporária para gameplay sem LevelId explícito.
        /// </summary>
        [System.Obsolete("Use RestartAsync(reason) para replay rápido ou StartGameplayAsync(levelId, reason) quando houver seleção de fase.")]
        Task RequestGameplayAsync(string reason = null);
    }
}
