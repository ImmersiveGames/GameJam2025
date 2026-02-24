using System;
using _ImmersiveGames.NewScripts.Core.Composition;
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

        public PostGameOwnershipExitContext(
            string signature,
            string sceneName,
            string profile,
            int frame,
            string reason,
            string nextState)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Reason = reason;
            NextState = nextState;
        }
    }

    /// <summary>
    /// Implementação padrão que aplica InputMode UI e gate de simulação no PostGame.
    /// </summary>
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
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<PostGameOwnershipService>(
                    "[PostGame] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][InputMode] Request mode='FrontendMenu' map='UI' phase='PostGame' reason='PostGame/Entered' " +
                $"signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            inputMode.SetFrontendMenu("PostGame/Entered");
        }

        private void ApplyExitInputMode(PostGameOwnershipExitContext context)
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<PostGameOwnershipService>(
                    "[PostGame] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            bool applyGameplay = context.NextState == "Playing";
            string modeName = applyGameplay ? "Gameplay" : "FrontendMenu";
            string mapName = applyGameplay ? "Player" : "UI";

            DebugUtility.Log<PostGameOwnershipService>(
                $"[OBS][InputMode] Request mode='{modeName}' map='{mapName}' phase='PostGameExit' reason='PostGame/{context.Reason}' " +
                $"signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            if (applyGameplay)
            {
                inputMode.SetGameplay($"PostGame/{context.Reason}");
            }
            else
            {
                inputMode.SetFrontendMenu($"PostGame/{context.Reason}");
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
                DebugUtility.LogWarning<PostGameOwnershipService>(
                    "[PostGame] ISimulationGateService indisponível. Gate não será adquirido.");
                return;
            }

            _gateHandle = gateService.Acquire(PostGameGateToken);
            DebugUtility.Log<PostGameOwnershipService>(
                $"[PostGame] Gate adquirido token='{PostGameGateToken}'.",
                DebugUtility.Colors.Info);
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
                DebugUtility.LogWarning<PostGameOwnershipService>(
                    $"[PostGame] Falha ao liberar gate ({reason}): {ex}");
            }

            _gateHandle = null;

            DebugUtility.Log<PostGameOwnershipService>(
                $"[PostGame] Gate liberado token='{PostGameGateToken}'.",
                DebugUtility.Colors.Info);
        }

        private static IInputModeService ResolveInputModeService()
        {
            return DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service)
                ? service
                : null;
        }

        private static ISimulationGateService ResolveGateService()
        {
            return DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service)
                ? service
                : null;
        }
    }
}
