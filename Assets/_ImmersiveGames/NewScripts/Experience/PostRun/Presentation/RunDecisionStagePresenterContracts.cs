using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation
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
