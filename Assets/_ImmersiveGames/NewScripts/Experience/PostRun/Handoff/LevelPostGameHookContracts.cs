using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Handoff
{
    public readonly struct LevelPostRunHookContext
    {
        public LevelPostRunHookContext(
            LevelDefinitionAsset levelRef,
            string levelSignature,
            string postGameSignature,
            string sceneName,
            PostRunResult result,
            string reason,
            int frame)
        {
            LevelRef = levelRef;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            PostRunSignature = string.IsNullOrWhiteSpace(postGameSignature) ? string.Empty : postGameSignature.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Result = result;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Frame = frame < 0 ? 0 : frame;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public string LevelSignature { get; }
        public string PostRunSignature { get; }
        public string SceneName { get; }
        public PostRunResult Result { get; }
        public string Reason { get; }
        public int Frame { get; }
    }

    public interface ILevelPostRunHookService
    {
        Task RunReactionAsync(LevelPostRunHookContext context, CancellationToken cancellationToken = default);
    }
}

