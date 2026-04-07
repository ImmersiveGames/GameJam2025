using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation
{
    public interface IRunResultStagePresenter
    {
        string PresenterSignature { get; }
        bool IsReady { get; }
        void AttachToRunResultStage(RunResultStage stage, IRunResultStageControl control);
        void DetachFromRunResultStage(string reason);
    }

    public interface IRunResultStagePresenterRegistry
    {
        bool HasPresenter { get; }
        bool TryGetCurrentPresenter(out IRunResultStagePresenter presenter);
        bool TryEnsureCurrentPresenter(_ImmersiveGames.NewScripts.Experience.PostRun.Contracts.RunResultStage stage, IRunResultStageControl control, string source, out IRunResultStagePresenter presenter);
    }

    public interface IRunResultStagePresenterHost : IRunResultStagePresenterRegistry
    {
        bool TryAdoptPresenter(IRunResultStagePresenter presenter, string source);
        bool TryDetachCurrentPresenter(string reason);
        bool TryUnregisterPresenter(IRunResultStagePresenter presenter, string reason);
    }
}
