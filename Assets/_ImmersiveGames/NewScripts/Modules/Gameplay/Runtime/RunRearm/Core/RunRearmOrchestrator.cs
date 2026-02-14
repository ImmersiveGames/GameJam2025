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
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core
{
    /// <summary>
    /// Orquestra GameplayReset (Cleanup/Restore/Rebind) por alvo (Players/Eater/ActorIdSet/All).
    /// Implementação de gameplay (não infra).
    /// </summary>
    public sealed class RunRearmOrchestrator : IRunRearmOrchestrator
    {
        private readonly string _sceneName;
        private readonly List<IActor> _actorBuffer = new(32);
        private readonly List<ResetTarget> _targets = new(32);
        private readonly List<IRunRearmable> _resettableBuffer = new(64);
        private readonly List<ResetEntry> _orderedResets = new(64);

        private readonly SemaphoreSlim _mutex = new(1, 1);
        private int _requestSerial;

        private bool _dependenciesResolved;
        private IActorRegistry _actorRegistry;
        private IRunRearmTargetClassifier _classifier;

        private IWorldResetPolicy _worldResetPolicy;
        private IActorDiscoveryStrategy _registryDiscovery;
        private IActorDiscoveryStrategy _sceneScanDiscovery;

        private bool _lastDiscoveryUsedSceneScan;
        private bool _lastDiscoveryScanDisabled;
        private bool _lastDiscoveryFallbackUsed;

        public bool IsResetInProgress { get; private set; }

        public RunRearmOrchestrator(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public async Task<bool> RequestResetAsync(RunRearmRequest request)
        {
            if (!await _mutex.WaitAsync(0))
            {
                DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
                    $"Gameplay reset ignored (in progress). request={request}");
                return false;
            }

            IsResetInProgress = true;
            try
            {
                EnsureDependencies();

                int serial = Interlocked.Increment(ref _requestSerial);
                string reason = string.IsNullOrWhiteSpace(request.Reason) ? "GameplayReset/Request" : request.Reason;

                var normalized = new RunRearmRequest(request.Target, reason, request.ActorIds, request.ActorKind);

                DebugUtility.Log(typeof(RunRearmOrchestrator),
                    $"[GameplayReset] Start: {normalized} (scene='{GetEffectiveSceneName()}', serial={serial})");

                BuildTargets(normalized);

                if (_lastDiscoveryUsedSceneScan && _targets.Count > 0)
                {
                    DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
                        $"[{ResetLogTags.Recovered}][RECOVERED] Scene scan discovery used (policy opt-in). request={normalized}");

                    _worldResetPolicy?.ReportDegraded(
                        ResetFeatureIds.GameplayReset,
                        "SceneScanDiscoveryUsed",
                        normalized.ToString());
                }

                if (_lastDiscoveryFallbackUsed && _targets.Count > 0)
                {
                    if (_worldResetPolicy != null && !_worldResetPolicy.AllowLegacyActorKindFallback)
                    {
                        DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
                            $"[{ResetLogTags.ValidationFailed}][STRICT_VIOLATION] Legacy actor-kind fallback usado, mas policy bloqueia. request={normalized}");
                    }

                    DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
                        $"[{ResetLogTags.Recovered}][RECOVERED] String-based fallback used for EaterOnly. request={normalized}");

                    _worldResetPolicy?.ReportDegraded(
                        ResetFeatureIds.GameplayReset,
                        "EaterFallbackUsed",
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
                        DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
                            $"[{ResetLogTags.ValidationFailed}][STRICT_VIOLATION] {msg}");
                        throw new InvalidOperationException(msg);
                    }

                    DebugUtility.LogWarning(typeof(RunRearmOrchestrator),
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

                DebugUtility.Log(typeof(RunRearmOrchestrator),
                    $"[GameplayReset] Completed: {normalized} (targets={_targets.Count}, serial={serial})");

                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(RunRearmOrchestrator),
                    $"[GameplayReset] Failed. request={request}, ex={ex}");
                throw;
            }
            finally
            {
                IsResetInProgress = false;
                _mutex.Release();
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
                _classifier = new DefaultRunRearmTargetClassifier();
            }

            provider.TryGetGlobal<IWorldResetPolicy>(out _worldResetPolicy);
            if (_worldResetPolicy == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);
                _worldResetPolicy = new ProductionWorldResetPolicy(runtimeModeProvider, degradedModeReporter);
            }

            _registryDiscovery = new RegistryActorDiscoveryStrategy(_actorRegistry, _classifier);
            _sceneScanDiscovery = new SceneScanActorDiscoveryStrategy(scene);

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

        private void BuildTargets(RunRearmRequest request)
        {
            _targets.Clear();
            _lastDiscoveryUsedSceneScan = false;
            _lastDiscoveryScanDisabled = false;
            _lastDiscoveryFallbackUsed = false;

            // 1) Tenta via ActorRegistry + classifier (r?pido, determin?stico)
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

                // Importante: se registry existe, mas est? vazio (ou classifier n?o achou),
                // fazemos fallback por scan somente quando a policy permitir.
            }

            // 2) Fallback (sem registry ou sem dados): scan da cena por IActor (opt-in por policy).
            if (_worldResetPolicy != null && _worldResetPolicy.AllowSceneScan && _sceneScanDiscovery != null)
            {
                _actorBuffer.Clear();
                _sceneScanDiscovery.CollectTargets(request, _actorBuffer, out bool fallbackUsed);
                _lastDiscoveryUsedSceneScan = true;
                _lastDiscoveryFallbackUsed = fallbackUsed;

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

        private async Task ResetOneTargetAsync(ResetTarget target, RunRearmRequest request, int serial)
        {
            string actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;

            IReadOnlyList<ResetEntry> components = ResolveResettableComponents(target, request);
            if (components.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(RunRearmOrchestrator),
                    $"[GameplayReset] No resettable components for actorId={actorId}.");
                return;
            }

            await RunStepAsync(components, RunRearmStep.Cleanup, request, serial);
            await RunStepAsync(components, RunRearmStep.Restore, request, serial);
            await RunStepAsync(components, RunRearmStep.Rebind, request, serial);
        }

        private IReadOnlyList<ResetEntry> ResolveResettableComponents(ResetTarget target, RunRearmRequest request)
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

                if (mb is IRunRearmable resettable)
                {
                    if (resettable is IRunRearmTargetFilter filter &&
                        !filter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(resettable);
                    continue;
                }

                if (mb is IRunRearmableSync sync)
                {
                    if (sync is IRunRearmTargetFilter filter &&
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
                int order = component is IRunRearmOrder resetOrder ? resetOrder.ResetOrder : 0;
                _orderedResets.Add(new ResetEntry(component, order));
            }

            _orderedResets.Sort(CompareResetEntries);

            return _orderedResets;
        }

        private async Task RunStepAsync(IReadOnlyList<ResetEntry> components, RunRearmStep step, RunRearmRequest request, int serial)
        {
            var ctx = new RunRearmContext(
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

        private static Task InvokeStepAsync(IRunRearmable component, RunRearmStep step, RunRearmContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return step switch
            {
                RunRearmStep.Cleanup => component.ResetCleanupAsync(ctx),
                RunRearmStep.Restore => component.ResetRestoreAsync(ctx),
                RunRearmStep.Rebind => component.ResetRebindAsync(ctx),
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
            public ResetEntry(IRunRearmable component, int order)
            {
                Component = component;
                Order = order;
            }

            public IRunRearmable Component { get; }
            public int Order { get; }
        }

        private sealed class SyncAdapter : IRunRearmable
        {
            private readonly IRunRearmableSync _sync;

            public SyncAdapter(IRunRearmableSync sync)
            {
                _sync = sync;
            }

            public Task ResetCleanupAsync(RunRearmContext ctx)
            {
                _sync.ResetCleanup(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRestoreAsync(RunRearmContext ctx)
            {
                _sync.ResetRestore(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRebindAsync(RunRearmContext ctx)
            {
                _sync.ResetRebind(ctx);
                return Task.CompletedTask;
            }
        }
    }
}


