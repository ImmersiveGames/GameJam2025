using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    public enum PostStageCompletionKind
    {
        Unknown = 0,
        Complete = 1,
        Skip = 2,
    }

    public readonly struct PostStageContext
    {
        public PostStageContext(string signature, string sceneName, int frame, GameRunOutcome outcome, string reason, bool isGameplayScene)
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

    public readonly struct PostStageCompletionResult
    {
        public PostStageCompletionResult(PostStageCompletionKind kind, string reason)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public PostStageCompletionKind Kind { get; }
        public string Reason { get; }
        public bool WasSkipped => Kind == PostStageCompletionKind.Skip;
        public bool WasCompleted => Kind == PostStageCompletionKind.Complete;
    }

    public interface IPostStageControlService
    {
        bool IsActive { get; }
        bool HasCompleted { get; }
        PostStageContext CurrentContext { get; }
        bool TryBegin(PostStageContext context);
        bool TryComplete(string reason = null);
        bool TrySkip(string reason = null);
        Task<PostStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Rail interno/transitório do PostRun para orquestrar a saída.
    ///
    /// Não é um segundo rail público do GameLoop; a superfície canônica externa
    /// continua sendo o ExitStage exposto pelo bridge de fim de run.
    /// </summary>
    public interface IPostStageCoordinator
    {
        Task RunAsync(PostStageContext context, CancellationToken cancellationToken = default);
    }

    public readonly struct PostStageStartRequestedEvent : IEvent
    {
        public PostStageStartRequestedEvent(PostStageContext context)
        {
            Context = context;
        }

        public PostStageContext Context { get; }
    }

    public readonly struct PostStageStartedEvent : IEvent
    {
        public PostStageStartedEvent(PostStageContext context)
        {
            Context = context;
        }

        public PostStageContext Context { get; }
    }

    public readonly struct PostStageCompletedEvent : IEvent
    {
        public PostStageCompletedEvent(PostStageContext context, PostStageCompletionResult completion)
        {
            Context = context;
            Completion = completion;
        }

        public PostStageContext Context { get; }
        public PostStageCompletionResult Completion { get; }
    }
}

