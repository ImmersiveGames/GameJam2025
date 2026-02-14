using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Trilho oficial F4 para iniciar gameplay por levelId.
    /// - Resolve LevelId -> RouteId via ILevelFlowService.
    /// - Dispara navegação sem montar scene lists.
    /// - Não decide reset de mundo (responsabilidade de F2 por policy de rota).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelFlowRuntimeService : ILevelFlowRuntimeService
    {
        private readonly ILevelFlowService _levelResolver;
        private readonly IGameNavigationService _navigationService;

        public LevelFlowRuntimeService(
            ILevelFlowService levelResolver,
            IGameNavigationService navigationService)
        {
            _levelResolver = levelResolver ?? throw new ArgumentNullException(nameof(levelResolver));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        public async Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var typedLevelId = LevelId.FromName(levelId);
            if (!typedLevelId.IsValid)
            {
                FailFastConfig($"LevelId inválido para StartGameplayAsync. levelId='{levelId ?? "<null>"}', reason='{reason ?? "<null>"}'.");
                return;
            }

            if (!_levelResolver.TryResolve(typedLevelId, out var resolvedRouteId, out var payload) || !resolvedRouteId.IsValid)
            {
                FailFastConfig($"LevelId não resolvido no LevelFlow. levelId='{typedLevelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayRequested levelId='{typedLevelId}', routeId='{resolvedRouteId}', reason='{reason ?? "<null>"}', signature='<pending>'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.StartGameplayRouteAsync(resolvedRouteId, payload, reason);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayDispatched routeId='{resolvedRouteId}', reason='{reason ?? "<null>"}', signature='<from-navigation>'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<LevelFlowRuntimeService>(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
