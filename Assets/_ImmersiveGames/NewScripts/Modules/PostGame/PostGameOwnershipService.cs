using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.InputModes;
namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    public readonly struct PostGameOwnershipContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }

        public PostGameOwnershipContext(string signature, string sceneName, string profile, int frame)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
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

        public PostGameOwnershipExitContext(string signature, string sceneName, string profile, int frame, string reason, string nextState)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Reason = reason;
            NextState = nextState;
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

        public void OnPostGameEntered(PostGameOwnershipContext context)
        {
            if (_isActive)
            {
                return;
            }

            _isActive = true;
            ApplyPostGameInputMode(context);
            AcquireGate();
        }

        public void OnPostGameExited(PostGameOwnershipExitContext context)
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            ReleaseGate(context.Reason);
            ApplyExitInputMode(context);
        }

        private static void ApplyPostGameInputMode(PostGameOwnershipContext context)
        {
            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][InputMode] Request mode='FrontendMenu' map='UI' phase='PostGame' reason='PostGame/Entered' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
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
                $"[OBS][InputMode] Request mode='{modeName}' map='{mapName}' phase='PostGameExit' reason='{reason}' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(applyGameplay ? InputModeRequestKind.Gameplay : InputModeRequestKind.FrontendMenu, reason, "PostGame", context.Signature));
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
