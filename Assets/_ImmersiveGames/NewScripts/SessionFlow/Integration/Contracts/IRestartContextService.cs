using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts
{
    public interface IRestartContextService
    {
        GameplayStartSnapshot Current { get; }
        GameplayStartSnapshot UpdateGameplayStartSnapshot(GameplayStartSnapshot snapshot);
        bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot);
        GameplayStartSnapshot RegisterGameplayStart(GameplayStartSnapshot snapshot);
        bool TryGetCurrent(out GameplayStartSnapshot snapshot);
        void Clear(string reason = null);
    }

    /// <summary>
    /// Boundary operacional de navegacao para o seam de SessionIntegration.
    /// SessionIntegration publica handoff; a implementacao aplica efeito final fora do seam.
    /// </summary>
    public interface ISessionIntegrationNavigationHandoffService
    {
        SceneRouteId ResolveGameplayRouteIdOrFail(string reason, string source);

        Task RequestStartGameplayRouteAsync(
            SceneRouteId routeId,
            string reason,
            string source,
            CancellationToken ct = default);

        Task RequestExitToMenuAsync(
            string reason,
            string source,
            CancellationToken ct = default);
    }
}

