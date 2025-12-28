// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn.QA
{
    /// <summary>
    /// QA harness para validar o pipeline "WorldDefinition -> SpawnRegistry -> WorldLifecycle -> ActorRegistry".
    /// Objetivo: confirmar que o hard reset executa Spawn/Despawn via serviços registrados e que o ActorRegistry reflete os atores.
    ///
    /// Uso típico:
    /// - Atribuir um WorldDefinition no NewSceneBootstrapper da cena (com entries Enabled + Prefab válido).
    /// - Entrar em Play Mode.
    /// - Rodar: "QA/SpawnPipeline/Run Hard Reset (Spawn)" no menu de contexto deste componente.
    /// </summary>
    [DisallowMultipleComponent]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class WorldSpawnPipelineQaRunner : MonoBehaviour
    {
        [Header("References (optional)")]
        [Tooltip("Se vazio, o runner tentará localizar um WorldLifecycleController na cena.")]
        [SerializeField] private WorldLifecycleController controller;

        [Tooltip("Quando true, executa automaticamente um hard reset ao iniciar.")]
        [SerializeField] private bool autoRunOnStart;

        [Header("Assertions")]
        [Min(0)]
        [SerializeField] private int expectedMinActorsAfterSpawn = 1;

        [Tooltip("Quando true, exige ao menos um PlayerActor registrado no ActorRegistry após o spawn.")]
        [SerializeField] private bool requirePlayerActor;

        private bool _running;
        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            if (controller == null)
            {
                controller = FindAnyObjectByType<WorldLifecycleController>(FindObjectsInactive.Include);
            }
        }

        private void Start()
        {
            if (!autoRunOnStart)
            {
                return;
            }

            _ = RunHardResetAsync("QA/SpawnPipeline/AutoRun");
        }

        [ContextMenu("QA/SpawnPipeline/Run Hard Reset (Spawn)")]
        private void ContextRunHardReset()
        {
            _ = RunHardResetAsync("QA/SpawnPipeline/HardReset");
        }

        [ContextMenu("QA/SpawnPipeline/Run Two Cycles (Hard Reset x2)")]
        private void ContextRunTwoCycles()
        {
            _ = RunTwoCyclesAsync();
        }

        [ContextMenu("QA/SpawnPipeline/Log Scene Spawn Wiring")]
        private void ContextLogWiring()
        {
            LogWiring();
        }

        private void LogWiring()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetForScene<IWorldSpawnServiceRegistry>(_sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] IWorldSpawnServiceRegistry não encontrado para a cena '{_sceneName}'. " +
                    "Confirme se o NewSceneBootstrapper existe nessa cena e se o Awake() dele executou.");
                return;
            }

            provider.TryGetForScene<IActorRegistry>(_sceneName, out var actorRegistry);

            DebugUtility.Log(typeof(WorldSpawnPipelineQaRunner),
                $"[QA][SpawnPipeline] Wiring => scene='{_sceneName}', spawnServices={spawnRegistry.Services.Count}, " +
                $"actorRegistryCount={actorRegistry?.Count ?? -1}, " +
                $"controller={(controller != null ? controller.gameObject.name : "<null>")}");

            for (int i = 0; i < spawnRegistry.Services.Count; i++)
            {
                var service = spawnRegistry.Services[i];
                DebugUtility.Log(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] SpawnService[{i}] => name='{service?.Name ?? "<null>"}', type='{service?.GetType().Name ?? "<null>"}'");
            }
        }

        private async Task RunTwoCyclesAsync()
        {
            if (_running)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] Ignorado: já em execução. scene='{_sceneName}'.");
                return;
            }

            await RunHardResetAsync("QA/SpawnPipeline/Cycle1");
            await Task.Yield();
            await RunHardResetAsync("QA/SpawnPipeline/Cycle2");
        }

        private async Task RunHardResetAsync(string reason)
        {
            if (_running)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] Ignorado: já em execução. reason='{reason}', scene='{_sceneName}'.");
                return;
            }

            _running = true;

            try
            {
                if (!TryResolveSceneServices(out var spawnRegistry, out var actorRegistry))
                {
                    LogWiring();
                    return;
                }

                if (spawnRegistry.Services.Count == 0)
                {
                    DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                        $"[QA][SpawnPipeline] SpawnRegistry vazio (scene='{_sceneName}'). " +
                        "Isso normalmente significa que o WorldDefinition não está atribuído no NewSceneBootstrapper " +
                        "ou que todas as entries estão Disabled / Prefab null.");
                    LogWiring();
                    return;
                }

                if (controller == null)
                {
                    controller = FindAnyObjectByType<WorldLifecycleController>(FindObjectsInactive.Include);
                }

                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldSpawnPipelineQaRunner),
                        $"[QA][SpawnPipeline] WorldLifecycleController não encontrado na cena '{_sceneName}'. " +
                        "Adicione o WorldLifecycleController (geralmente no WorldRoot) para executar o ciclo.");
                    return;
                }

                DebugUtility.Log(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] Run hard reset => reason='{reason}', scene='{_sceneName}'");

                await controller.ResetWorldAsync(reason);

                ValidateAfterSpawn(spawnRegistry, actorRegistry, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] Exception during QA run. scene='{_sceneName}', reason='{reason}'. ex={ex}");
            }
            finally
            {
                _running = false;
            }
        }

        private bool TryResolveSceneServices(out IWorldSpawnServiceRegistry spawnRegistry, out IActorRegistry actorRegistry)
        {
            spawnRegistry = null;
            actorRegistry = null;

            var provider = DependencyManager.Provider;

            if (!provider.TryGetForScene(_sceneName, out spawnRegistry) || spawnRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] IWorldSpawnServiceRegistry não encontrado para a cena '{_sceneName}'. " +
                    "Confirme se o NewSceneBootstrapper existe nessa cena e executou.");
                return false;
            }

            if (!provider.TryGetForScene(_sceneName, out actorRegistry) || actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    $"[QA][SpawnPipeline] IActorRegistry não encontrado para a cena '{_sceneName}'. " +
                    "O spawn pode até instanciar prefabs, mas o registry não refletirá os atores.");
            }

            return true;
        }

        private void ValidateAfterSpawn(
            IWorldSpawnServiceRegistry spawnRegistry,
            IActorRegistry actorRegistry,
            string reason)
        {
            int actorCount = actorRegistry?.Count ?? -1;

            bool passMinActors = actorCount >= expectedMinActorsAfterSpawn;

            bool hasPlayerActor = true;
            if (requirePlayerActor)
            {
                hasPlayerActor = actorRegistry != null && actorRegistry.Actors.Any(actor => actor is PlayerActor);
            }

            bool pass = passMinActors && hasPlayerActor;

            var color = pass ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning;

            DebugUtility.Log(typeof(WorldSpawnPipelineQaRunner),
                $"[QA][SpawnPipeline] {(pass ? "PASS" : "FAIL")} => " +
                $"spawnServices={spawnRegistry.Services.Count}, actorCount={actorCount}, expectedMinActors={expectedMinActorsAfterSpawn}, " +
                $"requirePlayerActor={requirePlayerActor}, hasPlayerActor={hasPlayerActor}. " +
                $"reason='{reason}', scene='{_sceneName}'",
                color);

            if (!pass)
            {
                DebugUtility.LogWarning(typeof(WorldSpawnPipelineQaRunner),
                    "[QA][SpawnPipeline] Dica: se spawnServices=0, atribua um WorldDefinition no NewSceneBootstrapper " +
                    "e ative ao menos uma entry Enabled com Prefab válido (ex.: Kind=Player + prefab com PlayerActor).");
            }
        }
    }
}
#endif
