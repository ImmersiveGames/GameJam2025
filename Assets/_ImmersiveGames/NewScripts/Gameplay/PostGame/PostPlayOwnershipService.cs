using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    /// <summary>
    /// Owner do input/gate do PostPlay, evitando duplicidade com overlays.
    /// </summary>
    public interface IPostPlayOwnershipService
    {
        bool IsOwnerEnabled { get; }
        void OnPostPlayEntered(PostPlayOwnershipContext context);
        void OnPostPlayExited(PostPlayOwnershipExitContext context);
    }

    public readonly struct PostPlayOwnershipContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }

        public PostPlayOwnershipContext(string signature, string sceneName, string profile, int frame)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
        }
    }

    public readonly struct PostPlayOwnershipExitContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public string Reason { get; }
        public string NextState { get; }

        public PostPlayOwnershipExitContext(
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
    /// Implementação padrão que aplica InputMode UI e gate de simulação no PostPlay.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostPlayOwnershipService : IPostPlayOwnershipService
    {
        private const string PostGameGateToken = "state.postgame";

        private bool _isActive;
        private bool _loggedMissingGate;
        private IDisposable _gateHandle;

        public bool IsOwnerEnabled => true;

        public void OnPostPlayEntered(PostPlayOwnershipContext context)
        {
            if (_isActive)
            {
                return;
            }

            _isActive = true;
            ApplyPostPlayInputMode(context);
            AcquireGate();
        }

        public void OnPostPlayExited(PostPlayOwnershipExitContext context)
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            ReleaseGate(context.Reason);
            ApplyExitInputMode(context);
        }

        private void ApplyPostPlayInputMode(PostPlayOwnershipContext context)
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<PostPlayOwnershipService>(
                    "[PostPlay] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            DebugUtility.Log<PostPlayOwnershipService>(
                $"[OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='PostPlay' reason='PostPlay/Entered' " +
                $"signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            inputMode.SetFrontendMenu("PostPlay/Entered");
        }

        private void ApplyExitInputMode(PostPlayOwnershipExitContext context)
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<PostPlayOwnershipService>(
                    "[PostPlay] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            var applyGameplay = context.NextState == "Playing";
            var modeName = applyGameplay ? "Gameplay" : "FrontendMenu";
            var mapName = applyGameplay ? "Player" : "UI";

            DebugUtility.Log<PostPlayOwnershipService>(
                $"[OBS][InputMode] Apply mode='{modeName}' map='{mapName}' phase='PostPlayExit' reason='PostPlay/{context.Reason}' " +
                $"signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame}.",
                DebugUtility.Colors.Info);

            if (applyGameplay)
            {
                inputMode.SetGameplay($"PostPlay/{context.Reason}");
            }
            else
            {
                inputMode.SetFrontendMenu($"PostPlay/{context.Reason}");
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
                DebugUtility.LogWarning<PostPlayOwnershipService>(
                    "[PostPlay] ISimulationGateService indisponível. Gate não será adquirido.");
                return;
            }

            _gateHandle = gateService.Acquire(PostGameGateToken);
            DebugUtility.Log<PostPlayOwnershipService>(
                $"[PostPlay] Gate adquirido token='{PostGameGateToken}'.",
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
            catch (System.Exception ex)
            {
                DebugUtility.LogWarning<PostPlayOwnershipService>(
                    $"[PostPlay] Falha ao liberar gate ({reason}): {ex}");
            }

            _gateHandle = null;

            DebugUtility.Log<PostPlayOwnershipService>(
                $"[PostPlay] Gate liberado token='{PostGameGateToken}'.",
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
