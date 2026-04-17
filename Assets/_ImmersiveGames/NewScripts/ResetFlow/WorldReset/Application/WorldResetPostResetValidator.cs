using System;
using System.Collections.Generic;
using System.Linq;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.Spawn;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Policies;
using UnityEngine.SceneManagement;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Application
{
    /// <summary>
    /// Valida as pós-condições mínimas do hard reset macro.
    /// Não executa reset nem decide policy; apenas verifica o estado pós-reset.
    /// </summary>
    public sealed class WorldResetPostResetValidator
    {
        private readonly IDependencyProvider _provider;

        public WorldResetPostResetValidator(IDependencyProvider provider)
        {
            _provider = provider;
        }

        public void ValidateEssentialActors(string targetScene, IWorldResetPolicy policy)
        {
            string sceneName = !string.IsNullOrWhiteSpace(targetScene)
                ? targetScene
                : SceneManager.GetActiveScene().name ?? string.Empty;

            if (_provider == null)
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] IDependencyProvider ausente. Não é possível validar pós-condições do hard reset. scene='{sceneName}'.");
                return;
            }

            if (!_provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                LogDegraded(policy,
                    $"IActorRegistry não disponível para scene='{sceneName}'. Não é possível validar presença mínima de actors após o reset.");
                return;
            }

            if (!_provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                LogDegraded(policy,
                    $"IWorldSpawnServiceRegistry não disponível para scene='{sceneName}'. Não é possível validar contratos essenciais de spawn.");
                return;
            }

            HashSet<ActorKind> presentActorKinds = CollectPresentActorKinds(actorRegistry);
            IReadOnlyList<IWorldSpawnService> essentialServices = CollectEssentialSpawnServices(spawnRegistry, sceneName, policy);
            if (essentialServices.Count == 0)
            {
                return;
            }

            var missingKinds = new List<ActorKind>();
            for (int i = 0; i < essentialServices.Count; i++)
            {
                IWorldSpawnService service = essentialServices[i];
                ActorKind actorKind = service.SpawnedActorKind;

                if (presentActorKinds.Contains(actorKind))
                {
                    DebugUtility.LogVerbose<WorldResetPostResetValidator>(
                        $"[WorldResetPostResetValidator] Pós-condição satisfeita. kind={actorKind}, service={DescribeService(service)}",
                        DebugUtility.Colors.Info);
                    continue;
                }

                missingKinds.Add(actorKind);
            }

            if (missingKinds.Count == 0)
            {
                DebugUtility.LogVerbose<WorldResetPostResetValidator>(
                    $"[OBS][WorldReset] Post-reset essential actor validation passed. scene='{sceneName}', required={essentialServices.Count}, presentKinds={presentActorKinds.Count}.",
                    DebugUtility.Colors.Success);
                return;
            }

            string missingKindsText = string.Join(", ", missingKinds.Select(static kind => kind.ToString()));
            string detail = $"Hard reset finalizou sem garantir actors essenciais. scene='{sceneName}', missingKinds=[{missingKindsText}]";

            if (policy != null && policy.IsStrict)
            {
                policy.ReportDegraded(
                    ResetFeatureIds.WorldReset,
                    "MissingEssentialActorsAfterReset",
                    detail,
                    signature: sceneName,
                    profile: policy.Name);

                throw new InvalidOperationException(detail);
            }

            LogDegraded(policy, detail, reason: "MissingEssentialActorsAfterReset", signature: sceneName, profile: policy?.Name);
        }

        private static HashSet<ActorKind> CollectPresentActorKinds(IActorRegistry actorRegistry)
        {
            var presentKinds = new HashSet<ActorKind>();
            if (actorRegistry == null)
            {
                return presentKinds;
            }

            var actorsList = new List<IActor>();
            actorRegistry.GetActors(actorsList);

            for (int i = 0; i < actorsList.Count; i++)
            {
                IActor actor = actorsList[i];
                if (actor is not IActorKindProvider kindProvider)
                {
                    continue;
                }

                ActorKind kind = kindProvider.Kind;
                if (kind == ActorKind.Unknown)
                {
                    continue;
                }

                presentKinds.Add(kind);
            }

            return presentKinds;
        }

        private static IReadOnlyList<IWorldSpawnService> CollectEssentialSpawnServices(
            IWorldSpawnServiceRegistry spawnRegistry,
            string sceneName,
            IWorldResetPolicy policy)
        {
            var essentialServices = new List<IWorldSpawnService>();
            var seenKinds = new HashSet<ActorKind>();

            if (spawnRegistry?.Services == null)
            {
                return essentialServices;
            }

            for (int i = 0; i < spawnRegistry.Services.Count; i++)
            {
                IWorldSpawnService service = spawnRegistry.Services[i];
                if (service == null || !service.IsRequiredForWorldReset)
                {
                    continue;
                }

                ActorKind actorKind = service.SpawnedActorKind;
                if (actorKind == ActorKind.Unknown)
                {
                    LogDegraded(policy,
                        $"Serviço essencial com ActorKind.Unknown detectado em scene='{sceneName}'. service={DescribeService(service)}.");
                    continue;
                }

                if (!seenKinds.Add(actorKind))
                {
                    LogDegraded(policy,
                        $"Serviços essenciais duplicados para ActorKind='{actorKind}' em scene='{sceneName}'. service={DescribeService(service)}.");
                    continue;
                }

                essentialServices.Add(service);
            }

            if (essentialServices.Count == 0)
            {
                LogDegraded(policy,
                    $"Nenhum serviço essencial registrado em scene='{sceneName}'. O hard reset não conseguirá validar presença mínima de actors.");
            }

            return essentialServices;
        }

        private static string DescribeService(IWorldSpawnService service)
        {
            if (service == null)
            {
                return "<null>";
            }

            string serviceName = string.IsNullOrWhiteSpace(service.Name)
                ? service.GetType().Name
                : service.Name;

            return $"{serviceName}(kind={service.SpawnedActorKind}, required={service.IsRequiredForWorldReset})";
        }

        private static void LogDegraded(
            IWorldResetPolicy policy,
            string message,
            string reason = "WorldResetDegraded",
            string signature = null,
            string profile = null)
        {
            if (policy != null && policy.IsStrict)
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][STRICT_VIOLATION] {message}");
            }
            else
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {message}");
            }

            policy?.ReportDegraded(
                ResetFeatureIds.WorldReset,
                reason,
                message,
                signature,
                profile);
        }
    }
}

