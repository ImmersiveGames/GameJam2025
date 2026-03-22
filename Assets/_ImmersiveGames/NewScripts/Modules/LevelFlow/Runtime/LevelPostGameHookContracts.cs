using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.PostGame;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelPostGameHookContext
    {
        public LevelPostGameHookContext(
            LevelDefinitionAsset levelRef,
            string levelSignature,
            string postGameSignature,
            string sceneName,
            PostGameResult result,
            string reason,
            int frame)
        {
            LevelRef = levelRef;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            PostGameSignature = string.IsNullOrWhiteSpace(postGameSignature) ? string.Empty : postGameSignature.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Result = result;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Frame = frame < 0 ? 0 : frame;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public string LevelSignature { get; }
        public string PostGameSignature { get; }
        public string SceneName { get; }
        public PostGameResult Result { get; }
        public string Reason { get; }
        public int Frame { get; }
    }

    public interface ILevelPostGameHookService
    {
        Task RunReactionAsync(LevelPostGameHookContext context, CancellationToken cancellationToken = default);
    }
}
