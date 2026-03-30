using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Strategy;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
{
    /// <summary>
    /// Orquestra GameplayReset (Cleanup/Restore/Rebind) por grupo canônico de atores.
    /// </summary>
    public sealed class ActorGroupRearmOrchestrator : IActorGroupRearmOrchestrator
    {
        private readonly string _sceneName;
        private readonly SemaphoreSlim _mutex = new(1, 1);
        private int _requestSerial;

        private bool _dependenciesResolved;
        private IActorRegistry _actorRegistry;
        private IActorGroupRearmTargetClassifier _classifier;

        private IWorldResetPolicy _worldResetPolicy;
        private IActorGroupRearmDiscoveryStrategy _registryDiscovery;
        private IActorGroupRearmDiscoveryStrategy _sceneScanDiscovery;

        private ActorGroupRearmTargetResolver _targetResolver;
        private ActorGroupRearmComponentResolver _componentResolver;
        private ActorGroupRearmExecutor _executor;

        public bool IsResetInProgress { get; private set; }

        public ActorGroupRearmOrchestrator(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public async Task<bool> RequestResetAsync(ActorGroupRearmRequest request)
        {
            if (!await _mutex.WaitAsync(0))
            {
                DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
                    $"Gameplay reset ignored (in progress). request={request}");
                return false;
            }

            IsResetInProgress = true;
            try
            {
                EnsureDependencies();
                ValidateRequest(request);

                int serial = Interlocked.Increment(ref _requestSerial);
                string reason = string.IsNullOrWhiteSpace(request.Reason) ? "GameplayReset/Request" : request.Reason;
                var normalized = new ActorGroupRearmRequest(request.Target, reason, request.ActorIds, request.ActorKind);

                DebugUtility.Log(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] Start: {normalized} (scene='{GetEffectiveSceneName()}', serial={serial})");

                var targets = _targetResolver.ResolveTargets(normalized, out bool usedSceneScan, out bool scanDisabled);

                if (usedSceneScan && targets.Count > 0)
                {
                    DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
                        $"[{ResetLogTags.Recovered}][RECOVERED] Scene scan discovery used (policy opt-in). request={normalized}");

                    _worldResetPolicy?.ReportDegraded(
                        ResetFeatureIds.GameplayReset,
                        "SceneScanDiscoveryUsed",
                        normalized.ToString());
                }

                if (targets.Count == 0)
                {
                    string detail = scanDisabled
                        ? "Scene scan disabled by policy."
                        : "No targets resolved by registry/scan.";
                    string msg = $"[GameplayReset] No targets resolved. {detail} request={normalized}";

                    if (_worldResetPolicy != null && _worldResetPolicy.IsStrict)
                    {
                        DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
                            $"[{ResetLogTags.ValidationFailed}][STRICT_VIOLATION] {msg}");
                        throw new InvalidOperationException(msg);
                    }

                    DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
                        $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {msg}");
                    _worldResetPolicy?.ReportDegraded(
                        ResetFeatureIds.GameplayReset,
                        "GameplayReset_NoTargets",
                        msg);
                    return true;
                }

                foreach (var target in targets)
                {
                    await ResetOneTargetAsync(target, normalized, serial);
                }

                DebugUtility.Log(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] Completed: {normalized} (targets={targets.Count}, serial={serial})");

                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] Failed. request={request}, ex={ex}");
                throw;
            }
            finally
            {
                IsResetInProgress = false;
                _mutex.Release();
            }
        }

        private static void ValidateRequest(ActorGroupRearmRequest request)
        {
            switch (request.Target)
            {
                case ActorGroupRearmTarget.ByActorKind:
                    if (request.ActorKind == ActorKind.Unknown)
                    {
                        throw new ArgumentException("ActorGroupRearmTarget.ByActorKind requer ActorKind válido.", nameof(request));
                    }
                    break;

                case ActorGroupRearmTarget.ActorIdSet:
                    if (request.ActorIds == null || request.ActorIds.Count == 0)
                    {
                        throw new ArgumentException("ActorGroupRearmTarget.ActorIdSet requer ao menos um ActorId.", nameof(request));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request.Target, "ActorGroupRearmTarget não suportado.");
            }
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            var provider = DependencyManager.Provider;
            string scene = GetEffectiveSceneName();

            provider.TryGetForScene(scene, out _actorRegistry);

            if (!provider.TryGetForScene(scene, out _classifier) || _classifier == null)
            {
                _classifier = new ActorGroupRearmDefaultTargetClassifier();
            }

            provider.TryGetGlobal(out _worldResetPolicy);
            if (_worldResetPolicy == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);
                _worldResetPolicy = new ProductionWorldResetPolicy(runtimeModeProvider, degradedModeReporter);
            }

            _registryDiscovery = new ActorGroupRearmRegistryDiscoveryStrategy(_actorRegistry, _classifier);
            _sceneScanDiscovery = new ActorGroupRearmSceneScanDiscoveryStrategy(scene);

            _targetResolver = new ActorGroupRearmTargetResolver(scene, _worldResetPolicy, _registryDiscovery, _sceneScanDiscovery);
            _componentResolver = new ActorGroupRearmComponentResolver(scene);
            _executor = new ActorGroupRearmExecutor(scene);

            _dependenciesResolved = true;
        }

        private string GetEffectiveSceneName()
        {
            if (!string.IsNullOrWhiteSpace(_sceneName))
            {
                return _sceneName;
            }

            return SceneManager.GetActiveScene().name;
        }

        private async Task ResetOneTargetAsync(ResetTarget target, ActorGroupRearmRequest request, int serial)
        {
            string actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;
            var components = _componentResolver.ResolveResettableComponents(target, request);
            if (components.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] No resettable components for actorId={actorId}.");
                return;
            }

            await _executor.RunAllStepsAsync(components, request, serial);
        }
    }
}
