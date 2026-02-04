#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.World;
using _ImmersiveGames.NewScripts.Runtime.World.Spawn;
using _ImmersiveGames.NewScripts.Runtime.Actors;

namespace _ImmersiveGames.NewScripts.Runtime.World
{
    public sealed class ResetWorldService : IResetWorldService
    {
        private readonly object _lock = new();
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);

        public async Task TriggerResetAsync(string? contextSignature, string? reason)
        {
            string ctx = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature;
            string rsn = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason;

            lock (_lock)
            {
                if (!_inFlight.Add(ctx))
                {
                    DebugUtility.LogWarning<ResetWorldService>($"[ResetWorldService] Reset já em andamento para signature='{ctx}'. Ignorando duplicate.");
                    return;
                }

            }

            try
            {
                DebugUtility.LogVerbose<ResetWorldService>($"[OBS][WorldLifecycle] ResetWorldStarted signature='{ctx}' reason='{rsn}'.", DebugUtility.Colors.Info);
                EventBus<WorldLifecycleResetStartedEvent>.Raise(new WorldLifecycleResetStartedEvent(ctx, rsn));

                // Fase 1: reset global (controller-driven)
                await ExecuteResetOnControllersAsync(rsn);

                // Fase 2: spawn essentials (Player + Eater) — delegate para spawn services in controllers
                await EnsureEssentialSpawnsAsync();

                // Fase 3: rearm / post-reset hooks (hook registry can run tasks)
                await RunRearmHooksAsync();

                DebugUtility.LogVerbose<ResetWorldService>($"[OBS][WorldLifecycle] ResetCompleted signature='{ctx}' reason='{rsn}'.", DebugUtility.Colors.Success);
                EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ResetWorldService>($"[ResetWorldService] Falha durante ResetWorldService.TriggerResetAsync signature='{ctx}' reason='{rsn}' ex={ex}");

                // Best-effort: ainda publica completed para evitar deadlock no SceneFlow gate.
                EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));
            }
            finally
            {
                lock (_lock)
                {
                    _inFlight.Remove(ctx);
                }
            }
        }

        private static async Task ExecuteResetOnControllersAsync(string reason)
        {
            // Localiza controllers na cena ativa e dispara ResetWorldAsync em cada um, preservando ordem determinística.
            string targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? string.Empty;
            IReadOnlyList<WorldLifecycleController>? controllers = WorldLifecycleControllerLocator.FindControllersForScene(targetScene);

            if (controllers == null || controllers.Count == 0)
            {
                DebugUtility.LogWarning<ResetWorldService>($"[ResetWorldService] Nenhum WorldLifecycleController encontrado para scene='{targetScene}'. Pulando reset controllers.");
                return;
            }

            var filtered = new List<WorldLifecycleController>(controllers.Count);
            foreach (var c in controllers)
            {
                if (c != null)
                {
                    filtered.Add(c);
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

        private static async Task EnsureEssentialSpawnsAsync()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? string.Empty;
            var provider = DependencyManager.Provider;

            if (!provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                DebugUtility.LogWarning<ResetWorldService>($"[ResetWorldService] IActorRegistry não disponível para scene='{sceneName}'. Não é possível verificar spawns essenciais.");
                return;
            }

            if (!provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                DebugUtility.LogWarning<ResetWorldService>($"[ResetWorldService] IWorldSpawnServiceRegistry não disponível para scene='{sceneName}'. Não é possível disparar spawn de serviços.");
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
            IWorldSpawnService? playerService = null;
            IWorldSpawnService? eaterService = null;
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
                DebugUtility.LogVerbose<ResetWorldService>($"[OBS][WorldLifecycle] Player ausente; disparando spawn via {playerService.Name}.", DebugUtility.Colors.Info);
                try { await playerService.SpawnAsync(); }
                catch (Exception ex) { DebugUtility.LogError<ResetWorldService>($"[ResetWorldService] Falha ao spawnar Player: {ex}"); }
            }
            else
            {
                DebugUtility.LogVerbose<ResetWorldService>($"[ResetWorldService] Player já presente ou serviço ausente. present={playerPresent}, service={(playerService==null?"<null>":playerService.Name)}", DebugUtility.Colors.Info);
            }

            if (!eaterPresent && eaterService != null)
            {
                DebugUtility.LogVerbose<ResetWorldService>($"[OBS][WorldLifecycle] Eater ausente; disparando spawn via {eaterService.Name}.", DebugUtility.Colors.Info);
                try { await eaterService.SpawnAsync(); }
                catch (Exception ex) { DebugUtility.LogError<ResetWorldService>($"[ResetWorldService] Falha ao spawnar Eater: {ex}"); }
            }
            else
            {
                DebugUtility.LogVerbose<ResetWorldService>($"[ResetWorldService] Eater já presente ou serviço ausente. present={eaterPresent}, service={(eaterService==null?"<null>":eaterService.Name)}", DebugUtility.Colors.Info);
            }
        }

        private static Task RunRearmHooksAsync()
        {
            // Placeholder for post-reset hooks (e.g., rearming systems, telemetry warmups)
            DebugUtility.LogVerbose<ResetWorldService>($"[ResetWorldService] RunRearmHooks (placeholder).", DebugUtility.Colors.Info);
            return Task.CompletedTask;
        }
    }
}


