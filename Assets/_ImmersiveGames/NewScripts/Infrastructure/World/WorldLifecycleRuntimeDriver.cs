using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Driver runtime que reage a SceneTransitionScenesReadyEvent e, quando aplicável,
    /// dispara hard reset do WorldLifecycle.
    ///
    /// Regra recomendada:
    /// - Profile "startup" (Frontend/Ready) NÃO roda reset de world/spawn.
    /// - Para gameplay profiles, executa ResetWorldAsync no controller da cena alvo.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeDriver : IDisposable
    {
        private const string StartupProfileName = "startup";

        // Observação: mantemos MenuScene como "cena frontend" por regra atual do projeto,
        // mas evitamos usar "menu" na nomenclatura (conceito é NonGameplay/Frontend).
        private const string FrontendSceneName = "MenuScene";

        private const string CompletedReasonSkipped = "Skipped_StartupOrFrontend";
        private const string CompletedReasonNoController = "Failed_NoController";

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
            string profile = evt.Context.TransitionProfileName ?? string.Empty;

            string activeSceneName =
                !string.IsNullOrWhiteSpace(evt.Context.TargetActiveScene)
                    ? evt.Context.TargetActiveScene
                    : SceneManager.GetActiveScene().name;

            string reason = $"ScenesReady/{activeSceneName}";

            try
            {
                // Regra: startup/frontend não faz reset de world.
                if (IsFrontendProfile(profile) || IsFrontendScene(activeSceneName))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Reset SKIPPED (startup/frontend). profile='{profile}', activeScene='{activeSceneName}'.",
                        DebugUtility.Colors.Info);

                    RaiseCompleted(evt.Context, CompletedReasonSkipped);
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

                    RaiseCompleted(evt.Context, CompletedReasonNoController);
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

        private static bool IsFrontendProfile(string profile)
        {
            return string.Equals(profile, StartupProfileName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFrontendScene(string sceneName)
        {
            return string.Equals(sceneName, FrontendSceneName, StringComparison.Ordinal);
        }

        private static WorldLifecycleController FindControllerInScene(string sceneName)
        {
            WorldLifecycleController[] controllers = Object.FindObjectsByType<WorldLifecycleController>(FindObjectsSortMode.None);

            return (from c in controllers where c != null let s = c.gameObject.scene where s.IsValid() && s.isLoaded && string.Equals(s.name, sceneName, StringComparison.Ordinal) select c).FirstOrDefault();

        }

        private static void RaiseCompleted(SceneTransitionContext context, string reason)
        {
            string signature = context.ToString();
            string profile = context.TransitionProfileName ?? string.Empty;

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='{profile}', signature='{signature}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature, reason));
        }
    }
}
