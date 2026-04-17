using ImmersiveGames.GameJam2025.Experience.PostRun.Contracts;

namespace ImmersiveGames.GameJam2025.Experience.PostRun.Presentation
{
    public interface IRunDecisionStagePresenter
    {
        string PresenterSignature { get; }
        bool IsReady { get; }
        void BindToRunDecision(RunDecision decision);
        void DetachFromRunDecision(string reason);
    }

    public interface IRunDecisionStagePresenterRegistry
    {
        bool HasPresenter { get; }
        bool TryGetCurrentPresenter(out IRunDecisionStagePresenter presenter);
    }

    public interface IRunDecisionStagePresenterHost : IRunDecisionStagePresenterRegistry
    {
        bool TryAdoptPresenter(IRunDecisionStagePresenter presenter, string source);
        bool TryReleasePresenter(IRunDecisionStagePresenter presenter, string reason);
    }
}

