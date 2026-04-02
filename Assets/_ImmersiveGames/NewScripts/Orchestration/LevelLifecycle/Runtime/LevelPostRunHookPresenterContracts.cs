#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface ILevelPostRunHookPresenter
    {
        string PresenterSignature { get; }
        bool IsReady { get; }
        void BindToSession(LevelPostRunHookContext context);
        Task WaitForCompletionAsync(CancellationToken cancellationToken = default);
    }

    public interface ILevelPostRunHookPresenterRegistry
    {
        bool TryGetCurrentPresenter(out ILevelPostRunHookPresenter presenter);
        bool TryEnsureCurrentPresenter(LevelPostRunHookContext context, string source, out ILevelPostRunHookPresenter presenter);
        void Register(ILevelPostRunHookPresenter presenter, string sessionSignature);
        void Unregister(ILevelPostRunHookPresenter presenter);
    }

    public interface ILevelPostRunHookPresenterScopeResolver
    {
        bool TryResolvePresenters(LevelPostRunHookContext context, out IReadOnlyList<ILevelPostRunHookPresenter> presenters);
    }
}
