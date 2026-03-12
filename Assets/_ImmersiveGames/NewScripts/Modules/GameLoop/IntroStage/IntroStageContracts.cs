#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage
{
    /// <summary>
    /// Contexto minimo para execucao da IntroStageController antes da revelacao da cena.
    /// RouteKind e a semantica canonica; labels sao apenas observabilidade.
    /// </summary>
    public readonly struct IntroStageContext
    {
        public string ContextSignature { get; }
        public SceneRouteKind RouteKind { get; }
        public string ProfileLabel { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public IntroStageContext(
            string? contextSignature,
            SceneRouteKind routeKind,
            string? profileLabel,
            string? targetScene,
            string? reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            RouteKind = routeKind;
            ProfileLabel = profileLabel ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public enum IntroStagePolicy
    {
        Disabled,
        Manual,
        AutoComplete
    }

    public interface IIntroStagePolicyResolver
    {
        IntroStagePolicy Resolve(SceneRouteKind routeKind, string reason);
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
