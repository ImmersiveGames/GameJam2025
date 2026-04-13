using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Discovery;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Execution;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Policies;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Observability;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Coordination
{
    /// <summary>
    /// Orquestra GameplayReset (Cleanup/Restore/Rebind) para atores ja vivos,
    /// materializados e registrados. Nao materializa nem assume ownership do objeto.
    /// </summary>
    public sealed class ActorGroupGameplayResetOrchestrator : IActorGroupGameplayResetOrchestrator
    {
        private readonly string _sceneName;
        private readonly SemaphoreSlim _mutex = new(1, 1);
        private int _requestSerial;

        private bool _dependenciesResolved;
        private IActorRegistry _actorRegistry;
        private IActorGroupGameplayResetTargetClassifier _classifier;

        private IActorGroupGameplayResetPolicy _policy;
        private IActorGroupGameplayResetDiscoveryStrategy _registryDiscovery;
        private IActorGroupGameplayResetDiscoveryStrategy _sceneScanDiscovery;

        private IActorGroupGameplayResetTargetResolver _targetResolver;
        private IActorGroupGameplayResetExecutor _executor;

        public bool IsResetInProgress { get; private set; }

        public ActorGroupGameplayResetOrchestrator(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public async Task<bool> RequestResetAsync(ActorGroupGameplayResetRequest request)
        {
            if (!await _mutex.WaitAsync(0))
            {
                DebugUtility.LogWarning(typeof(ActorGroupGameplayResetOrchestrator),
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
                var normalized = new ActorGroupGameplayResetRequest(request.Target, reason, request.ActorIds, request.ActorKind);

                DebugUtility.Log(typeof(ActorGroupGameplayResetOrchestrator),
                    $"[GameplayReset] Start: {normalized} (scene='{GetEffectiveSceneName()}', serial={serial})");

                var targets = _targetResolver.ResolveTargets(normalized, out bool usedSceneScan, out bool scanDisabled);

                if (usedSceneScan && targets.Count > 0)
                {
                    DebugUtility.LogWarning(typeof(ActorGroupGameplayResetOrchestrator),
                        $"[{ResetLogTags.Recovered}][RECOVERED] Scene scan discovery used (policy opt-in). request={normalized}");

                    _policy?.ReportDegraded(
                        GameplayResetFeatureIds.GameplayReset,
                        "SceneScanDiscoveryUsed",
                        normalized.ToString());
                }

                if (targets.Count == 0)
                {
                    string detail = scanDisabled
                        ? "Scene scan disabled by policy."
                        : "No targets resolved by registry/scan.";
                    string msg = $"[GameplayReset] No targets resolved. {detail} request={normalized}";

                    if (_policy != null && _policy.IsStrict)
                    {
                        DebugUtility.LogWarning(typeof(ActorGroupGameplayResetOrchestrator),
                            $"[{ResetLogTags.ValidationFailed}][STRICT_VIOLATION] {msg}");
                        throw new InvalidOperationException(msg);
                    }

                    DebugUtility.LogWarning(typeof(ActorGroupGameplayResetOrchestrator),
                        $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {msg}");
                    _policy?.ReportDegraded(
                        GameplayResetFeatureIds.GameplayReset,
                        "GameplayReset_NoTargets",
                        msg);
                    return true;
                }

                foreach (var target in targets)
                {
                    await ResetOneTargetAsync(target, normalized, serial);
                }

                DebugUtility.Log(typeof(ActorGroupGameplayResetOrchestrator),
                    $"[GameplayReset] Completed: {normalized} (targets={targets.Count}, serial={serial})");

                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(ActorGroupGameplayResetOrchestrator),
                    $"[GameplayReset] Failed. request={request}, ex={ex}");
                throw;
            }
            finally
            {
                IsResetInProgress = false;
                _mutex.Release();
            }
        }

        private static void ValidateRequest(ActorGroupGameplayResetRequest request)
        {
            switch (request.Target)
            {
                case ActorGroupGameplayResetTarget.ByActorKind:
                    if (request.ActorKind == ActorKind.Unknown)
                    {
                        throw new ArgumentException("ActorGroupGameplayResetTarget.ByActorKind requer ActorKind válido.", nameof(request));
                    }
                    break;

                case ActorGroupGameplayResetTarget.ActorIdSet:
                    if (request.ActorIds == null || request.ActorIds.Count == 0)
                    {
                        throw new ArgumentException("ActorGroupGameplayResetTarget.ActorIdSet requer ao menos um ActorId.", nameof(request));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request.Target, "ActorGroupGameplayResetTarget não suportado.");
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
                _classifier = new ActorGroupGameplayResetDefaultTargetClassifier();
            }

            provider.TryGetGlobal(out IActorGroupGameplayResetPolicy gameplayResetPolicy);
            if (gameplayResetPolicy == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);
                gameplayResetPolicy = new ProductionActorGroupGameplayResetPolicy(runtimeModeProvider, degradedModeReporter);
            }

            _policy = gameplayResetPolicy;
            _registryDiscovery = new ActorGroupGameplayResetRegistryDiscoveryStrategy(_actorRegistry, _classifier);
            _sceneScanDiscovery = new ActorGroupGameplayResetSceneScanDiscoveryStrategy(scene);

            _targetResolver = new ActorGroupGameplayResetTargetResolver(scene, _policy, _registryDiscovery, _sceneScanDiscovery);
            _executor = new ActorGroupGameplayResetExecutor(scene);

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

        private async Task ResetOneTargetAsync(ResetTarget target, ActorGroupGameplayResetRequest request, int serial)
        {
            string actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;
            DebugUtility.LogVerbose(typeof(ActorGroupGameplayResetOrchestrator),
                $"[GameplayReset] Executing target actorId={actorId}.");

            await _executor.ExecuteAsync(target, request, serial);
        }
    }
}

