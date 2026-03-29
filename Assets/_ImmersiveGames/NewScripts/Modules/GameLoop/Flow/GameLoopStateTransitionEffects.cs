using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.PostGame;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Flow
{
    /// <summary>
    /// Efeitos auxiliares do GameLoop.
    ///
    /// Slice 2:
    /// - este componente não publica semântica de ExitStage;
    /// - o rail canônico de saída é exposto pelo GameRunEndedEventBridge;
    /// - aqui ficam apenas efeitos/ponte internos para o PostRunMenu.
    /// </summary>
    public sealed class GameLoopStateTransitionEffects
    {
        private readonly GameLoopPostGameSnapshotResolver _snapshotResolver;

        public GameLoopStateTransitionEffects(GameLoopPostGameSnapshotResolver snapshotResolver)
        {
            _snapshotResolver = snapshotResolver;
        }

        public void HandlePostPlayEntered()
        {
            if (!_snapshotResolver.IsGameplayScene())
            {
                DebugUtility.LogWarning<GameLoopStateTransitionEffects>(
                    $"[OBS][PostRunMenu][Bridge] PostRunMenuBridgeSkipped reason='scene_not_gameplay' scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'.");
                return;
            }

            var info = _snapshotResolver.BuildSignatureInfo();
            var resultSnapshot = _snapshotResolver.ResolvePostGameSnapshot();

            DebugUtility.Log<GameLoopStateTransitionEffects>(
                $"[OBS][PostRunMenu][Bridge] PostRunMenuBridgeEntered signature='{info.Signature}' result='{resultSnapshot.Result}' reason='{resultSnapshot.Reason}' scene='{info.SceneName}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            var owner = _snapshotResolver.ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostGameEntered(new PostGameOwnershipContext(
                info.Signature,
                info.SceneName,
                string.Empty,
                info.Frame,
                resultSnapshot.Result,
                resultSnapshot.Reason));
        }

        public void HandlePostPlayExited(GameLoopStateId nextState, string reason)
        {
            var info = _snapshotResolver.BuildSignatureInfo();
            var resultSnapshot = _snapshotResolver.ResolvePostGameSnapshot();

            DebugUtility.Log<GameLoopStateTransitionEffects>(
                $"[OBS][PostRunMenu][Bridge] PostRunMenuBridgeExited signature='{info.Signature}' reason='{reason}' nextState='{nextState}' scene='{info.SceneName}' frame={info.Frame} result='{resultSnapshot.Result}'.",
                DebugUtility.Colors.Info);

            var owner = _snapshotResolver.ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostGameExited(new PostGameOwnershipExitContext(
                info.Signature,
                info.SceneName,
                string.Empty,
                info.Frame,
                reason,
                nextState.ToString(),
                resultSnapshot.Result));
        }

        public void ApplyGameplayInputMode()
        {
            var info = _snapshotResolver.BuildSignatureInfo();
            DebugUtility.Log<GameLoopStateTransitionEffects>(
                $"[OBS][InputMode] Request mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='{info.Signature}' scene='{info.SceneName}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.Gameplay, "GameLoop/Playing", "GameLoop", info.Signature));
        }
    }
}
