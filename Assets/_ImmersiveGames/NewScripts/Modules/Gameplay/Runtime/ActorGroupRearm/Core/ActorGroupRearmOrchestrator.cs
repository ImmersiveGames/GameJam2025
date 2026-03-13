using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    /// <summary>
    /// Orquestra GameplayReset (Cleanup/Restore/Rebind) por grupo canônico de atores.
    /// </summary>
    public sealed class ActorGroupRearmOrchestrator : IActorGroupRearmOrchestrator
    {
        private readonly string _sceneName;
        private readonly List<IActor> _actorBuffer = new(32);
        private readonly List<ResetTarget> _targets = new(32);
        private readonly List<IActorGroupRearmable> _resettableBuffer = new(64);
        private readonly List<ResetEntry> _orderedResets = new(64);

        private readonly SemaphoreSlim _mutex = new(1, 1);
        private int _requestSerial;

        private bool _dependenciesResolved;
        private IActorRegistry _actorRegistry;
        private IActorGroupRearmTargetClassifier _classifier;

        private IWorldResetPolicy _worldResetPolicy;
        private IActorGroupRearmDiscoveryStrategy _registryDiscovery;
        private IActorGroupRearmDiscoveryStrategy _sceneScanDiscovery;

        private bool _lastDiscoveryUsedSceneScan;
        private bool _lastDiscoveryScanDisabled;

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

                BuildTargets(normalized);

                if (_lastDiscoveryUsedSceneScan && _targets.Count > 0)
                {
                    DebugUtility.LogWarning(typeof(ActorGroupRearmOrchestrator),
                        $"[{ResetLogTags.Recovered}][RECOVERED] Scene scan discovery used (policy opt-in). request={normalized}");

                    _worldResetPolicy?.ReportDegraded(
                        ResetFeatureIds.GameplayReset,
                        "SceneScanDiscoveryUsed",
                        normalized.ToString());
                }

                if (_targets.Count == 0)
                {
                    string detail = _lastDiscoveryScanDisabled
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

                foreach (var target in _targets)
                {
                    await ResetOneTargetAsync(target, normalized, serial);
                }

                DebugUtility.Log(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] Completed: {normalized} (targets={_targets.Count}, serial={serial})");

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
                _classifier = new DefaultActorGroupRearmTargetClassifier();
            }

            provider.TryGetGlobal<IWorldResetPolicy>(out _worldResetPolicy);
            if (_worldResetPolicy == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);
                _worldResetPolicy = new ProductionWorldResetPolicy(runtimeModeProvider, degradedModeReporter);
            }

            _registryDiscovery = new RegistryActorGroupRearmDiscoveryStrategy(_actorRegistry, _classifier);
            _sceneScanDiscovery = new SceneScanActorGroupRearmDiscoveryStrategy(scene);

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

        private void BuildTargets(ActorGroupRearmRequest request)
        {
            _targets.Clear();
            _lastDiscoveryUsedSceneScan = false;
            _lastDiscoveryScanDisabled = false;

            if (_registryDiscovery != null)
            {
                _actorBuffer.Clear();
                _registryDiscovery.CollectTargets(request, _actorBuffer, out _);

                if (_actorBuffer.Count > 0)
                {
                    foreach (var t in _actorBuffer)
                    {
                        TryAddTargetFromActor(t);
                    }

                    SortTargets();
                    return;
                }
            }

            if (_worldResetPolicy != null && _worldResetPolicy.AllowSceneScan && _sceneScanDiscovery != null)
            {
                _actorBuffer.Clear();
                _sceneScanDiscovery.CollectTargets(request, _actorBuffer, out _);
                _lastDiscoveryUsedSceneScan = true;

                if (_actorBuffer.Count > 0)
                {
                    foreach (var t in _actorBuffer)
                    {
                        TryAddTargetFromActor(t);
                    }
                }
            }
            else
            {
                _lastDiscoveryScanDisabled = true;
            }

            SortTargets();
        }

        private void TryAddTargetFromActor(IActor actor)
        {
            if (actor == null)
            {
                return;
            }

            var t = actor.Transform;
            if (t == null)
            {
                return;
            }

            var root = t.gameObject;
            if (root == null)
            {
                return;
            }

            string sceneName = GetEffectiveSceneName();
            if (!string.IsNullOrWhiteSpace(sceneName) && root.scene.name != sceneName)
            {
                return;
            }

            string actorId = actor.ActorId ?? string.Empty;
            _targets.Add(new ResetTarget(actorId, root, t));
        }

        private void SortTargets()
        {
            if (_targets.Count <= 1)
            {
                return;
            }

            _targets.Sort((left, right) => string.CompareOrdinal(left.ActorId, right.ActorId));
        }

        private async Task ResetOneTargetAsync(ResetTarget target, ActorGroupRearmRequest request, int serial)
        {
            string actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;

            IReadOnlyList<ResetEntry> components = ResolveResettableComponents(target, request);
            if (components.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(ActorGroupRearmOrchestrator),
                    $"[GameplayReset] No resettable components for actorId={actorId}.");
                return;
            }

            await RunStepAsync(components, ActorGroupRearmStep.Cleanup, request, serial);
            await RunStepAsync(components, ActorGroupRearmStep.Restore, request, serial);
            await RunStepAsync(components, ActorGroupRearmStep.Rebind, request, serial);
        }

        private IReadOnlyList<ResetEntry> ResolveResettableComponents(ResetTarget target, ActorGroupRearmRequest request)
        {
            _orderedResets.Clear();
            _resettableBuffer.Clear();

            var root = target.Root;
            if (root == null)
            {
                return Array.Empty<ResetEntry>();
            }

            MonoBehaviour[] monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            if (monoBehaviours == null || monoBehaviours.Length == 0)
            {
                return Array.Empty<ResetEntry>();
            }

            foreach (var mb in monoBehaviours)
            {
                if (mb == null)
                {
                    continue;
                }

                if (mb is IActorGroupRearmable resettable)
                {
                    if (resettable is IActorGroupRearmTargetFilter filter &&
                        !filter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(resettable);
                    continue;
                }

                if (mb is IActorGroupRearmableSync sync)
                {
                    if (sync is IActorGroupRearmTargetFilter filter &&
                        !filter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(new SyncAdapter(sync));
                }
            }

            if (_resettableBuffer.Count == 0)
            {
                return Array.Empty<ResetEntry>();
            }

            foreach (var component in _resettableBuffer)
            {
                int order = component is IActorGroupRearmOrder resetOrder ? resetOrder.ResetOrder : 0;
                _orderedResets.Add(new ResetEntry(component, order));
            }

            _orderedResets.Sort(CompareResetEntries);

            return _orderedResets;
        }

        private async Task RunStepAsync(IReadOnlyList<ResetEntry> components, ActorGroupRearmStep step, ActorGroupRearmRequest request, int serial)
        {
            var ctx = new ActorGroupRearmContext(
                GetEffectiveSceneName(),
                request,
                serial,
                Time.frameCount,
                step);

            foreach (var t in components)
            {
                await InvokeStepAsync(t.Component, step, ctx);
            }
        }

        private static Task InvokeStepAsync(IActorGroupRearmable component, ActorGroupRearmStep step, ActorGroupRearmContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return step switch
            {
                ActorGroupRearmStep.Cleanup => component.ResetCleanupAsync(ctx),
                ActorGroupRearmStep.Restore => component.ResetRestoreAsync(ctx),
                ActorGroupRearmStep.Rebind => component.ResetRebindAsync(ctx),
                _ => Task.CompletedTask
            };
        }

        private static int CompareResetEntries(ResetEntry left, ResetEntry right)
        {
            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            string leftName = left.Component?.GetType().FullName;
            string rightName = right.Component?.GetType().FullName;

            int nameCompare = string.CompareOrdinal(leftName, rightName);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            int leftId = (left.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
            int rightId = (right.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
            return leftId.CompareTo(rightId);
        }

        private readonly struct ResetTarget
        {
            public ResetTarget(string actorId, GameObject root, Transform transform)
            {
                ActorId = actorId;
                Root = root;
                Transform = transform;
            }

            public string ActorId { get; }
            public GameObject Root { get; }
            public Transform Transform { get; }
        }

        private readonly struct ResetEntry
        {
            public ResetEntry(IActorGroupRearmable component, int order)
            {
                Component = component;
                Order = order;
            }

            public IActorGroupRearmable Component { get; }
            public int Order { get; }
        }

        private sealed class SyncAdapter : IActorGroupRearmable
        {
            private readonly IActorGroupRearmableSync _sync;

            public SyncAdapter(IActorGroupRearmableSync sync)
            {
                _sync = sync;
            }

            public Task ResetCleanupAsync(ActorGroupRearmContext ctx)
            {
                _sync.ResetCleanup(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRestoreAsync(ActorGroupRearmContext ctx)
            {
                _sync.ResetRestore(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRebindAsync(ActorGroupRearmContext ctx)
            {
                _sync.ResetRebind(ctx);
                return Task.CompletedTask;
            }
        }
    }
}

