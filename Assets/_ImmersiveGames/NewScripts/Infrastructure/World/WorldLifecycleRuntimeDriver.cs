using System;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Driver de produção para acionar reset determinístico do mundo no momento correto do Scene Flow.
    /// Reage ao SceneTransitionScenesReadyEvent garantindo idempotência.
    ///
    /// Nota:
    /// - NÃO adquire gate extra (flow.loading). O WorldLifecycleOrchestrator já adquire WorldLifecycle.WorldReset.
    /// - O gate macro do SceneFlow (flow.scene_transition) permanece sob responsabilidade do GameReadinessService.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeDriver
    {
        private readonly object _resetLock = new();

        private Task _ongoingResetTask;
        private string _lastResetContextSignature;

        private readonly EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;

        public WorldLifecycleRuntimeDriver()
        {
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnSceneTransitionScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);

            DebugUtility.LogVerbose<WorldLifecycleRuntimeDriver>("[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.");
        }

        public void RequestInitialReset(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/InitialReset", null, "RequestInitialReset", profileName: null);
        }

        public void RequestHardRestart(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/HardRestart", null, "RequestHardRestart", profileName: null);
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var signature = BuildContextSignature(evt.Context);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={evt.Context}");

            var activeScene = SceneManager.GetActiveScene().name;
            var target = string.IsNullOrWhiteSpace(evt.Context.TargetActiveScene) ? activeScene : evt.Context.TargetActiveScene;

            var reason = $"ScenesReady/{target}";
            var profile = evt.Context.TransitionProfileName;

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] Disparando hard reset após ScenesReady. reason='{reason}', profile='{profile ?? "<null>"}'");

            TriggerResetAsync(reason, signature, evt.Context, profileName: profile);
        }

        private void TriggerResetAsync(string reason, string contextSignature, object contextForLog, string profileName)
        {
            lock (_resetLock)
            {
                if (_ongoingResetTask != null && !_ongoingResetTask.IsCompleted)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Pedido de reset ignorado (já em andamento). reason='{reason}'. Context={contextForLog}");
                    return;
                }

                if (!TryMarkAndStoreContext(contextSignature))
                {
                    DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Reset ignorado (já executado para este contexto). Context={contextForLog}");
                    return;
                }

                _ongoingResetTask = RunResetAsync(reason, profileName);
            }
        }

        private async Task RunResetAsync(string reason, string profileName)
        {
            try
            {
                var controller = ResolveController();
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                        "[WorldLifecycle] WorldLifecycleController não encontrado. Reset abortado.");
                    return;
                }

                await controller.ResetWorldAsync(reason ?? "WorldLifecycleRuntimeDriver");

                // Sinaliza conclusão para liberar COMMAND start (coordinator).
                EventBus<_ImmersiveGames.NewScripts.Infrastructure.GameLoop.WorldLifecycleResetCompletedEvent>.Raise(
                    new _ImmersiveGames.NewScripts.Infrastructure.GameLoop.WorldLifecycleResetCompletedEvent(profileName, reason));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Erro durante reset: {ex}");
            }
            finally
            {
                lock (_resetLock)
                {
                    _ongoingResetTask = null;
                }
            }
        }

        private WorldLifecycleController ResolveController()
        {
            var activeScene = SceneManager.GetActiveScene().name;
            var foundController = Object.FindFirstObjectByType<WorldLifecycleController>();
            if (foundController == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] WorldLifecycleController não encontrado na cena ativa '{activeScene}'.");
            }

            return foundController;
        }

        private bool TryMarkAndStoreContext(string contextSignature)
        {
            if (string.IsNullOrEmpty(contextSignature))
                return true;

            if (string.Equals(_lastResetContextSignature, contextSignature, StringComparison.Ordinal))
                return false;

            _lastResetContextSignature = contextSignature;
            return true;
        }

        private static string BuildContextSignature(SceneTransitionContext context)
        {
            var targetScene = string.IsNullOrEmpty(context.TargetActiveScene) ? "<null>" : context.TargetActiveScene;
            var useFade = context.UseFade ? "1" : "0";
            var loadPart = BuildListPart(context.ScenesToLoad);
            var unloadPart = BuildListPart(context.ScenesToUnload);
            var profileId = string.IsNullOrWhiteSpace(context.TransitionProfileName) ? "<null>" : context.TransitionProfileName;

            return $"Target={targetScene};Fade={useFade};Load=[{loadPart}];Unload=[{unloadPart}];Profile={profileId}";
        }

        private static string BuildListPart(System.Collections.Generic.IReadOnlyList<string> list)
        {
            if (list == null)
                return "<null>";

            var filteredEntries = list.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            return filteredEntries.Length == 0 ? "<empty>" : string.Join("|", filteredEntries);
        }
    }
}
