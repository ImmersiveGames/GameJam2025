using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Scene;
namespace _ImmersiveGames.NewScripts.Runtime.Navigation
{
    /// <summary>
    /// Implementação de produção: executa rotas via ISceneTransitionService.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly GameNavigationCatalog _catalog;

        private int _navigationInProgress;

        public GameNavigationService(ISceneTransitionService sceneFlow)
            : this(sceneFlow, GameNavigationCatalog.CreateDefaultMinimal())
        {
        }

        public GameNavigationService(ISceneTransitionService sceneFlow, GameNavigationCatalog catalog)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[Navigation] GameNavigationService inicializado. Rotas: {string.Join(", ", _catalog.RouteIds)}",
                DebugUtility.Colors.Info);
        }

        public Task RequestMenuAsync(string reason = null)
            => NavigateAsync(GameNavigationCatalog.Routes.ToMenu, reason);

        public Task RequestGameplayAsync(string reason = null)
            => NavigateAsync(GameNavigationCatalog.Routes.ToGameplay, reason);

        public async Task NavigateAsync(string routeId, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    "[Navigation] NavigateAsync chamado com routeId vazio. Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navegação já em progresso. Ignorando rota='{routeId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(routeId, out var request) || request == null)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Rota desconhecida ou sem request. routeId='{routeId}'.");
                    return;
                }

                DebugUtility.Log(typeof(GameNavigationService),
                    $"[Navigation] NavigateAsync -> routeId='{routeId}', reason='{reason ?? "<null>"}', " +
                    $"Load=[{string.Join(", ", request.ScenesToLoad)}], " +
                    $"Unload=[{string.Join(", ", request.ScenesToUnload)}], " +
                    $"Active='{request.TargetActiveScene}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await _sceneFlow.TransitionAsync(request);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao navegar. routeId='{routeId}', ex={ex}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _navigationInProgress, 0);
            }
        }
    }
}
