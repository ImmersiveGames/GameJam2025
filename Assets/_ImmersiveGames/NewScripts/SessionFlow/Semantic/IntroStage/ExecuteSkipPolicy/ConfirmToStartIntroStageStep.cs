#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine.SceneManagement;
namespace ImmersiveGames.GameJam2025.Orchestration.GameLoop.IntroStage.Runtime
{
    /// <summary>
    /// Passo minimo da IntroStage que valida a ativacao canonica e deixa o wait
    /// operacional para o coordinator, via adapter de completion por execucao.
    /// </summary>
    public sealed class ConfirmToStartIntroStageStep : IIntroStageStep
    {
        public bool HasContent => true;

        public Task RunAsync(IntroStageContext context, CancellationToken cancellationToken)
        {
            _ = context;
            _ = cancellationToken;

            IIntroStageControlService controlService = ResolveIntroStageControlService();

            if (!controlService.IsIntroStageActive)
            {
                throw new InvalidOperationException("[FATAL][H1][GameLoop] ConfirmToStartIntroStageStep executado sem IntroStage ativa.");
            }

            DebugUtility.Log<ConfirmToStartIntroStageStep>(
                $"[OBS][IntroStageStep] Waiting for canonical IntroStage presenter confirmation. scene='{SceneManager.GetActiveScene().name}'.",
                DebugUtility.Colors.Info);

            return Task.CompletedTask;
        }

        private static IIntroStageControlService ResolveIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) && service != null)
            {
                return service;
            }

            throw new InvalidOperationException("IIntroStageControlService is required.");
        }
    }
}

