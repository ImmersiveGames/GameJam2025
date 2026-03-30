#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime
{
    /// <summary>
    /// Passo minimo da IntroStage que apenas aguarda a confirmacao canonica do presenter de level.
    /// </summary>
    public sealed class ConfirmToStartIntroStageStep : IIntroStageStep
    {
        public bool HasContent => true;

        public async Task RunAsync(IntroStageContext context, CancellationToken cancellationToken)
        {
            _ = context;

            IIntroStageControlService controlService = ResolveIntroStageControlService();

            DebugUtility.Log<ConfirmToStartIntroStageStep>(
                $"[OBS][EnterStageController] Waiting for canonical level presenter confirmation. scene='{SceneManager.GetActiveScene().name}'.",
                DebugUtility.Colors.Info);

            await controlService.WaitForCompletionAsync(cancellationToken);
        }

        private static IIntroStageControlService ResolveIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) && service != null)
            {
                return service;
            }

            throw new System.InvalidOperationException("IIntroStageControlService is required.");
        }
    }
}
