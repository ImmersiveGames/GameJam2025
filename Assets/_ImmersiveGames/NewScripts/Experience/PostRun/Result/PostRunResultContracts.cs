using System;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Result
{
    public enum PostRunResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Exit = 3,
    }

    public interface IPostRunResultService : IDisposable
    {
        bool HasResult { get; }
        PostRunResult Result { get; }
        string Reason { get; }
        void Clear(string reason = null);
        bool TrySetRunOutcome(GameRunOutcome outcome, string reason = null);
        bool TrySetExit(string reason = null);
    }
}

