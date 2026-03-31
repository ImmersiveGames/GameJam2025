using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    public readonly struct PostRunOwnershipContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public PostRunResult Result { get; }
        public string Reason { get; }

        public PostRunOwnershipContext(string signature, string sceneName, string profile, int frame, PostRunResult result, string reason)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Result = result;
            Reason = reason;
        }
    }

    public readonly struct PostRunOwnershipExitContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public string Reason { get; }
        public string NextState { get; }
        public PostRunResult Result { get; }

        public PostRunOwnershipExitContext(string signature, string sceneName, string profile, int frame, string reason, string nextState, PostRunResult result)
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
    public sealed class PostRunOwnershipService : IPostRunOwnershipService
    {
        private const string PostRunGateToken = "state.postgame";

        private bool _isActive;
        private bool _loggedMissingGate;
        private IDisposable _gateHandle;

        public bool IsOwnerEnabled => true;
        public bool IsActive => _isActive;

        public void OnPostRunEntered(PostRunOwnershipContext context)
        {
            if (_isActive)
            {
                return;
            }

            _isActive = true;
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][PostRunMenu] PostRunMenuEntered downstreamFrom='RunResult' signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            ApplyPostRunInputMode(context);
            AcquireGate();
            EventBus<PostRunEnteredEvent>.Raise(new PostRunEnteredEvent(context));

            if (context.Result == PostRunResult.Victory || context.Result == PostRunResult.Defeat)
            {
                _ = RunLevelHookSafelyAsync(context.Signature, context.SceneName, context.Result, context.Reason, context.Frame);
            }
        }

        public void OnPostRunExited(PostRunOwnershipExitContext context)
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][PostRunMenu] PostRunMenuExited downstreamTo='{context.NextState}' signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            ReleaseGate(context.Reason);
            ApplyExitInputMode(context);
            EventBus<PostRunExitedEvent>.Raise(new PostRunExitedEvent(context));

            if (context.Result == PostRunResult.Exit)
            {
                _ = RunLevelHookSafelyAsync(context.Signature, context.SceneName, context.Result, context.Reason, context.Frame);
            }
        }

        private static void ApplyPostRunInputMode(PostRunOwnershipContext context)
        {
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][InputMode] Request mode='FrontendMenu' map='UI' phase='PostRunMenu' reason='PostRunMenu/Entered' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.FrontendMenu, "PostRunMenu/Entered", "PostRunMenu", context.Signature));
        }

        private void ApplyExitInputMode(PostRunOwnershipExitContext context)
        {
            bool applyGameplay = context.NextState == "Playing";
            string modeName = applyGameplay ? "Gameplay" : "FrontendMenu";
            string mapName = applyGameplay ? "Player" : "UI";
            string reason = $"PostRunMenu/{context.Reason}";

            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][InputMode] Request mode='{modeName}' map='{mapName}' phase='PostRunMenuExit' reason='{reason}' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(applyGameplay ? InputModeRequestKind.Gameplay : InputModeRequestKind.FrontendMenu, reason, "PostRunMenu", context.Signature));
        }

        private async Task RunLevelHookSafelyAsync(string signature, string sceneName, PostRunResult result, string reason, int frame)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ILevelPostRunHookService>(out var hookService) || hookService == null)
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
                await hookService.RunReactionAsync(new LevelPostRunHookContext(
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
                DebugUtility.LogWarning<PostRunOwnershipService>(
                    $"[OBS][PostRun] LevelPostRunHookFailed levelRef='{contract.LevelRef.name}' result='{result}' ex='{ex.GetType().Name}: {ex.Message}'.");
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
                DebugUtility.LogWarning<PostRunOwnershipService>("[PostRunMenu] ISimulationGateService indisponivel. Gate nao sera adquirido.");
                return;
            }

            _gateHandle = gateService.Acquire(PostRunGateToken);
            DebugUtility.Log<PostRunOwnershipService>($"[PostRunMenu] Gate adquirido token='{PostRunGateToken}'.", DebugUtility.Colors.Info);
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
                DebugUtility.LogWarning<PostRunOwnershipService>($"[PostRunMenu] Falha ao liberar gate ({reason}): {ex}");
            }

            _gateHandle = null;
            DebugUtility.Log<PostRunOwnershipService>($"[PostRunMenu] Gate liberado token='{PostRunGateToken}'.", DebugUtility.Colors.Info);
        }

        private static ISimulationGateService ResolveGateService()
        {
            return DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service)
                ? service
                : null;
        }
    }
}

