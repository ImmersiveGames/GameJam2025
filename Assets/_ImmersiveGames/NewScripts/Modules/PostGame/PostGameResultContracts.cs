using System;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    public enum PostGameResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Exit = 3,
    }

    public interface IPostGameResultService : IDisposable
    {
        bool HasResult { get; }
        PostGameResult Result { get; }
        string Reason { get; }
        void Clear(string reason = null);
        bool TrySetExit(string reason = null);
    }
}
