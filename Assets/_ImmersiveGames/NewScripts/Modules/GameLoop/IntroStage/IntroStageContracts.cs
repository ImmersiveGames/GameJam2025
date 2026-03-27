#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage
{
    /// <summary>
    /// Contexto minimo para execucao da IntroStage antes da revelacao da cena.
    /// O contrato canonico de level vem de LevelFlow; este contexto apenas o transporta ate o executor.
    /// </summary>
    public readonly struct IntroStageContext
    {
        public LevelIntroStageSession Session { get; }
        public string ContextSignature { get; }
        public SceneRouteKind RouteKind { get; }
        public string TargetScene { get; }
        public string Reason { get; }
        public bool HasIntroStage => Session.HasIntroStage;
        public bool IsValid => Session.IsValid;

        public IntroStageContext(
            LevelIntroStageSession session,
            string? targetScene,
            string? reason)
        {
            Session = session;
            ContextSignature = session.LevelSignature;
            RouteKind = session.MacroRouteRef != null ? session.MacroRouteRef.RouteKind : SceneRouteKind.Unspecified;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public interface IIntroStageStep
    {
        bool HasContent { get; }
        Task RunAsync(IntroStageContext context, CancellationToken cancellationToken);
    }

    public interface IIntroStageCoordinator
    {
        Task RunIntroStageAsync(IntroStageContext context);
    }

    public interface IIntroStageControlService
    {
        bool IsIntroStageActive { get; }
        void BeginIntroStage(IntroStageContext context);
        Task<IntroStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken);
        void CompleteIntroStage(string reason);
        void SkipIntroStage(string reason);
        void MarkSessionClosed();
    }

    public readonly struct IntroStageCompletionResult
    {
        public string Reason { get; }
        public bool WasSkipped { get; }

        public IntroStageCompletionResult(string? reason, bool wasSkipped)
        {
            Reason = reason ?? string.Empty;
            WasSkipped = wasSkipped;
        }
    }

    public sealed class NoOpIntroStageStep : IIntroStageStep
    {
        public bool HasContent => false;

        public Task RunAsync(IntroStageContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}

