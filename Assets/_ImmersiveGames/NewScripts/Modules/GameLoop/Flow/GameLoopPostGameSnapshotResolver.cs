using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Flow
{
    /// <summary>
    /// Resolver bridge temporario para leitura downstream do resultado de fim de run.
    ///
    /// Não é owner de RunResult/PostRunMenu; apenas projeta o snapshot já consolidado
    /// em PostGame para efeitos de transição e UI downstream enquanto o backbone amadurece.
    /// </summary>
    public sealed class GameLoopPostGameSnapshotResolver
    {
        public IPostGameOwnershipService ResolvePostPlayOwnershipService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var service)
                ? service
                : null;
        }

        public IPostGameResultService ResolvePostGameResultService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var service)
                ? service
                : null;
        }

        public IGameRunResultSnapshotService ResolveGameRunStatus()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameRunResultSnapshotService>(out var status)
                ? status
                : null;
        }

        public bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return false;
        }

        public SignatureInfo BuildSignatureInfo()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string signature = "<none>";

            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) && cache != null &&
                cache.TryGetLast(out string cachedSignature, out string cachedScene))
            {
                signature = string.IsNullOrWhiteSpace(cachedSignature) ? "<none>" : cachedSignature.Trim();
                if (!string.IsNullOrWhiteSpace(cachedScene))
                {
                    sceneName = cachedScene;
                }
            }

            return new SignatureInfo(signature, sceneName, Time.frameCount);
        }

        public PostGameSnapshot ResolvePostGameSnapshot()
        {
            var postGameResult = ResolvePostGameResultService();
            if (postGameResult != null && postGameResult.HasResult)
            {
                return new PostGameSnapshot(postGameResult.Result, NormalizeValue(postGameResult.Reason));
            }

            var status = ResolveGameRunStatus();
            if (status?.HasResult == true)
            {
                PostGameResult fallbackResult = status.Outcome switch
                {
                    GameRunOutcome.Victory => PostGameResult.Victory,
                    GameRunOutcome.Defeat => PostGameResult.Defeat,
                    _ => PostGameResult.None,
                };

                return new PostGameSnapshot(fallbackResult, NormalizeValue(status.Reason));
            }

            return new PostGameSnapshot(PostGameResult.None, "<none>");
        }

        private static string NormalizeValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        public readonly struct SignatureInfo
        {
            public SignatureInfo(string signature, string sceneName, int frame)
            {
                Signature = string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();
                SceneName = string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
                Frame = frame;
            }

            public string Signature { get; }
            public string SceneName { get; }
            public int Frame { get; }
        }

        public readonly struct PostGameSnapshot
        {
            public PostGameSnapshot(PostGameResult result, string reason)
            {
                Result = result;
                Reason = string.IsNullOrWhiteSpace(reason) ? "<none>" : reason.Trim();
            }

            public PostGameResult Result { get; }
            public string Reason { get; }
        }
    }
}
