using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Driver runtime que reage a SceneTransitionScenesReadyEvent e, quando aplicável,
    /// dispara hard reset do WorldLifecycle.
    ///
    /// Regra recomendada:
    /// - Profile "startup" (Menu) NÃO roda reset de world/spawn (MenuScene é "sem infra").
    /// - Para gameplay profiles, executa ResetWorldAsync no controller da cena alvo.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeDriver : IDisposable
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;
        private int _resetInFlight; // 0/1

        public WorldLifecycleRuntimeDriver()
        {
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeDriver),
                "[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (Interlocked.CompareExchange(ref _resetInFlight, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                    "[WorldLifecycle] Reset já está em execução. Ignorando novo ScenesReady.");
                return;
            }

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={evt.Context}");

            _ = HandleScenesReadyAsync(evt);
        }

        private async Task HandleScenesReadyAsync(SceneTransitionScenesReadyEvent evt)
        {
            var profile = evt.Context.TransitionProfileName ?? string.Empty;

            var activeSceneName =
                !string.IsNullOrWhiteSpace(evt.Context.TargetActiveScene)
                    ? evt.Context.TargetActiveScene
                    : SceneManager.GetActiveScene().name;

            var reason = $"ScenesReady/{activeSceneName}";

            try
            {
                // Regra: Menu/startup não faz reset de world.
                if (string.Equals(profile, "startup", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(activeSceneName, "MenuScene", StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Reset SKIPPED (profile/menu). profile='{profile}', activeScene='{activeSceneName}'.",
                        DebugUtility.Colors.Info);

                    RaiseCompleted(evt.Context, "Skipped_StartupOrMenu");
                    return;
                }

                DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Disparando hard reset após ScenesReady. reason='{reason}', profile='{profile}'",
                    DebugUtility.Colors.Info);

                var controller = FindControllerInScene(activeSceneName);

                // Para gameplay scenes, controller é esperado.
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] WorldLifecycleController não encontrado na cena '{activeSceneName}'. Reset abortado.");

                    RaiseCompleted(evt.Context, "Failed_NoController");
                    return;
                }

                await controller.ResetWorldAsync(reason);

                RaiseCompleted(evt.Context, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Falha ao executar reset após ScenesReady. reason='{reason}', profile='{profile}', ex={ex}");

                RaiseCompleted(evt.Context, $"Failed_Reset:{ex.GetType().Name}");
            }
            finally
            {
                Interlocked.Exchange(ref _resetInFlight, 0);
            }
        }

        private static WorldLifecycleController FindControllerInScene(string sceneName)
        {
            var controllers = UnityEngine.Object.FindObjectsByType<WorldLifecycleController>(FindObjectsSortMode.None);

            foreach (var c in controllers)
            {
                if (c == null) continue;

                var s = c.gameObject.scene;
                if (s.IsValid() && s.isLoaded && string.Equals(s.name, sceneName, StringComparison.Ordinal))
                    return c;
            }

            return null;
        }

        private static void RaiseCompleted(SceneTransitionContext context, string reason)
        {
            var signature = context.ToString();
            var profile = context.TransitionProfileName ?? string.Empty;

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='{profile}', signature='{signature}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature, reason));
        }
    }
}
