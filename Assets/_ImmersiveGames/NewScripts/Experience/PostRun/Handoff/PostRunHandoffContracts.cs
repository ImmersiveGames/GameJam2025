using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    /// <summary>
    /// Contrato explicito do handoff entre o fim da run do GameLoop e a entrada no PostRun.
    /// </summary>
    public readonly struct PostRunHandoffContext
    {
        public PostRunHandoffContext(string signature, string sceneName, int frame, GameRunOutcome outcome, string reason, bool isGameplayScene)
        {
            Signature = string.IsNullOrWhiteSpace(signature) ? string.Empty : signature.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Frame = frame;
            Outcome = outcome;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            IsGameplayScene = isGameplayScene;
        }

        public string Signature { get; }
        public string SceneName { get; }
        public int Frame { get; }
        public GameRunOutcome Outcome { get; }
        public string Reason { get; }
        public bool IsGameplayScene { get; }
    }

    public interface IPostRunHandoffService
    {
        Task HandleRunEndedAsync(PostRunHandoffContext context, CancellationToken cancellationToken = default);
    }
}

