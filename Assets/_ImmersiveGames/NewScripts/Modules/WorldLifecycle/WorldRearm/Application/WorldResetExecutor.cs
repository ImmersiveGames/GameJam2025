using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Bindings;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application
{
    /// <summary>
    /// Executa a parte determinística do reset (controllers + spawns essenciais + rearm hooks).
    /// </summary>
    public sealed class WorldResetExecutor
    {
        private readonly IDependencyProvider _provider;

        public WorldResetExecutor(IDependencyProvider provider)
        {
            _provider = provider;
        }

        public async Task ExecuteAsync(
            WorldResetRequest request,
            IReadOnlyList<WorldLifecycleController> controllers,
            IWorldResetPolicy policy)
        {
            await ExecuteResetOnControllersAsync(controllers, request.Reason);
            await EnsureEssentialSpawnsAsync(request.TargetScene, policy);
            await RunRearmHooksAsync();
        }

        private static async Task ExecuteResetOnControllersAsync(
            IReadOnlyList<WorldLifecycleController> controllers,
            string reason)
        {
            if (controllers == null || controllers.Count == 0)
            {
                return;
            }

            var filtered = new List<WorldLifecycleController>(controllers.Count);
            for (int i = 0; i < controllers.Count; i++)
            {
                var controller = controllers[i];
                if (controller != null)
                {
                    filtered.Add(controller);
                }
            }

            filtered.Sort(static (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

            var tasks = new List<Task>(filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                tasks.Add(filtered[i].ResetWorldAsync(reason));
            }

            await Task.WhenAll(tasks);
        }

        private async Task EnsureEssentialSpawnsAsync(string targetScene, IWorldResetPolicy policy)
        {
            string sceneName = !string.IsNullOrWhiteSpace(targetScene)
                ? targetScene
                : SceneManager.GetActiveScene().name ?? string.Empty;

            if (_provider == null)
            {
                DebugUtility.LogWarning<WorldResetExecutor>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] IDependencyProvider ausente. Não é possível verificar spawns essenciais. scene='{sceneName}'.");
                return;
            }

            if (!_provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                LogDegraded(policy,
                    $"IActorRegistry não disponível para scene='{sceneName}'. Não é possível verificar spawns essenciais.");
                return;
            }

            if (!_provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                LogDegraded(policy,
                    $"IWorldSpawnServiceRegistry não disponível para scene='{sceneName}'. Não é possível disparar spawn de serviços.");
                return;
            }

            // Detecta se Player/Eater já existem pelo tipo do ator.
            var actorsList = new List<IActor>();
            actorRegistry.GetActors(actorsList);

            bool playerPresent = false;
            bool eaterPresent = false;
            foreach (var a in actorsList)
            {
                if (a == null)
                {
                    continue;
                }

                string typeName = a.GetType().Name;
                if (!playerPresent && typeName.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    playerPresent = true;
                }
                if (!eaterPresent && typeName.IndexOf("Eater", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    eaterPresent = true;
                }
                if (playerPresent && eaterPresent)
                {
                    break;
                }
            }

            // Localiza services correspondentes no registry por nome (convenção: PlayerSpawnService / EaterSpawnService).
            IWorldSpawnService playerService = null;
            IWorldSpawnService eaterService = null;
            foreach (var s in spawnRegistry.Services)
            {
                if (s == null)
                {
                    continue;
                }
                string name = s.Name ?? s.GetType().Name;
                if (playerService == null && name.IndexOf("PlayerSpawnService", StringComparison.Ordinal) >= 0)
                {
                    playerService = s;
                }
                if (eaterService == null && name.IndexOf("EaterSpawnService", StringComparison.Ordinal) >= 0)
                {
                    eaterService = s;
                }
            }

            // Executa spawn se necessário.
            if (!playerPresent && playerService != null)
            {
                DebugUtility.LogVerbose<WorldResetExecutor>(
                    $"[OBS][WorldLifecycle] Player ausente; disparando spawn via {playerService.Name}.",
                    DebugUtility.Colors.Info);
                try { await playerService.SpawnAsync(); }
                catch (Exception ex) { DebugUtility.LogError<WorldResetExecutor>($"[WorldResetExecutor] Falha ao spawnar Player: {ex}"); }
            }
            else
            {
                DebugUtility.LogVerbose<WorldResetExecutor>(
                    $"[WorldResetExecutor] Player já presente ou serviço ausente. present={playerPresent}, service={(playerService == null ? "<null>" : playerService.Name)}",
                    DebugUtility.Colors.Info);
            }

            if (!eaterPresent && eaterService != null)
            {
                DebugUtility.LogVerbose<WorldResetExecutor>(
                    $"[OBS][WorldLifecycle] Eater ausente; disparando spawn via {eaterService.Name}.",
                    DebugUtility.Colors.Info);
                try { await eaterService.SpawnAsync(); }
                catch (Exception ex) { DebugUtility.LogError<WorldResetExecutor>($"[WorldResetExecutor] Falha ao spawnar Eater: {ex}"); }
            }
            else
            {
                DebugUtility.LogVerbose<WorldResetExecutor>(
                    $"[WorldResetExecutor] Eater já presente ou serviço ausente. present={eaterPresent}, service={(eaterService == null ? "<null>" : eaterService.Name)}",
                    DebugUtility.Colors.Info);
            }
        }

        private static Task RunRearmHooksAsync()
        {
            // Placeholder for post-reset hooks (e.g., rearming systems, telemetry warmups)
            DebugUtility.LogVerbose<WorldResetExecutor>(
                $"[WorldResetExecutor] RunRearmHooks (placeholder).",
                DebugUtility.Colors.Info);
            return Task.CompletedTask;
        }

        private static void LogDegraded(IWorldResetPolicy policy, string message)
        {
            if (policy != null && policy.IsStrict)
            {
                DebugUtility.LogWarning<WorldResetExecutor>(
                    $"[{ResetLogTags.DegradedMode}][STRICT_VIOLATION] {message}");
            }
            else
            {
                DebugUtility.LogWarning<WorldResetExecutor>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {message}");
            }

            policy?.ReportDegraded(
                ResetFeatureIds.WorldReset,
                "WorldResetDegraded",
                message);
        }
    }
}
