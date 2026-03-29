using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    public readonly struct PostGameOwnershipContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public PostGameResult Result { get; }
        public string Reason { get; }

        public PostGameOwnershipContext(string signature, string sceneName, string profile, int frame, PostGameResult result, string reason)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Result = result;
            Reason = reason;
        }
    }

    public readonly struct PostGameOwnershipExitContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public string Reason { get; }
        public string NextState { get; }
        public PostGameResult Result { get; }

        public PostGameOwnershipExitContext(string signature, string sceneName, string profile, int frame, string reason, string nextState, PostGameResult result)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Reason = reason;
            NextState = nextState;
            Result = result;
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameOwnershipService : IPostGameOwnershipService
    {
        private const string PostGameGateToken = "state.postgame";

        private bool _isActive;
        private bool _loggedMissingGate;
        private IDisposable _gateHandle;

        public bool IsOwnerEnabled => true;
        public bool IsActive => _isActive;

        public void OnPostGameEntered(PostGameOwnershipContext context)
        {
            if (_isActive)
            {
                return;
            }

            _isActive = true;
            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][PostRunMenu] PostRunMenuEntered signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            ApplyPostGameInputMode(context);
            AcquireGate();
            EventBus<PostGameEnteredEvent>.Raise(new PostGameEnteredEvent(context));

            if (context.Result == PostGameResult.Victory || context.Result == PostGameResult.Defeat)
            {
                _ = RunLevelHookSafelyAsync(context.Signature, context.SceneName, context.Result, context.Reason, context.Frame);
            }
        }

        public void OnPostGameExited(PostGameOwnershipExitContext context)
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][PostRunMenu] PostRunMenuExited signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}' nextState='{context.NextState}'.",
                DebugUtility.Colors.Info);
            ReleaseGate(context.Reason);
            ApplyExitInputMode(context);
            EventBus<PostGameExitedEvent>.Raise(new PostGameExitedEvent(context));

            if (context.Result == PostGameResult.Exit)
            {
                _ = RunLevelHookSafelyAsync(context.Signature, context.SceneName, context.Result, context.Reason, context.Frame);
            }
        }

        private static void ApplyPostGameInputMode(PostGameOwnershipContext context)
        {
            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][InputMode] Request mode='FrontendMenu' map='UI' phase='PostGame' reason='PostGame/Entered' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.FrontendMenu, "PostGame/Entered", "PostGame", context.Signature));
        }

        private void ApplyExitInputMode(PostGameOwnershipExitContext context)
        {
            bool applyGameplay = context.NextState == "Playing";
            string modeName = applyGameplay ? "Gameplay" : "FrontendMenu";
            string mapName = applyGameplay ? "Player" : "UI";
            string reason = $"PostGame/{context.Reason}";

            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][InputMode] Request mode='{modeName}' map='{mapName}' phase='PostGameExit' reason='{reason}' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(applyGameplay ? InputModeRequestKind.Gameplay : InputModeRequestKind.FrontendMenu, reason, "PostGame", context.Signature));
        }

        private async Task RunLevelHookSafelyAsync(string signature, string sceneName, PostGameResult result, string reason, int frame)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ILevelPostGameHookService>(out var hookService) || hookService == null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) ||
                stagePresentationService == null ||
                !stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid)
            {
                return;
            }

            try
            {
                await hookService.RunReactionAsync(new LevelPostGameHookContext(
                    contract.LevelRef,
                    contract.LevelSignature,
                    signature,
                    sceneName,
                    result,
                    reason,
                    frame));
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOwnershipService>(
                    $"[OBS][PostGame] LevelPostGameHookFailed levelRef='{contract.LevelRef.name}' result='{result}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private void AcquireGate()
        {
            if (_gateHandle != null)
            {
                return;
            }

            var gateService = ResolveGateService();
            if (gateService == null)
            {
                if (_loggedMissingGate)
                {
                    return;
                }

                _loggedMissingGate = true;
                DebugUtility.LogWarning<PostGameOwnershipService>("[PostGame] ISimulationGateService indisponivel. Gate nao sera adquirido.");
                return;
            }

            _gateHandle = gateService.Acquire(PostGameGateToken);
            DebugUtility.Log<PostGameOwnershipService>($"[PostGame] Gate adquirido token='{PostGameGateToken}'.", DebugUtility.Colors.Info);
        }

        private void ReleaseGate(string reason)
        {
            if (_gateHandle == null)
            {
                return;
            }

            try
            {
                _gateHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOwnershipService>($"[PostGame] Falha ao liberar gate ({reason}): {ex}");
            }

            _gateHandle = null;
            DebugUtility.Log<PostGameOwnershipService>($"[PostGame] Gate liberado token='{PostGameGateToken}'.", DebugUtility.Colors.Info);
        }

        private static ISimulationGateService ResolveGateService()
        {
            return DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service)
                ? service
                : null;
        }
    }
}
