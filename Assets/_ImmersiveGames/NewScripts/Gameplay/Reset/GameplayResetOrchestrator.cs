using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Orquestra GameplayReset (Cleanup/Restore/Rebind) por alvo (Players/Eater/ActorIdSet/All).
    /// Implementação de gameplay (não infra).
    /// </summary>
    public sealed class GameplayResetOrchestrator : IGameplayResetOrchestrator
    {
        private readonly string _sceneName;
        private readonly List<IActor> _actorBuffer = new(32);
        private readonly List<ResetTarget> _targets = new(32);
        private readonly List<IGameplayResettable> _resettableBuffer = new(64);
        private readonly List<ResetEntry> _orderedResets = new(64);

        private readonly SemaphoreSlim _mutex = new(1, 1);
        private int _requestSerial;

        private bool _dependenciesResolved;
        private IActorRegistry _actorRegistry;
        private IGameplayResetTargetClassifier _classifier;

        public bool IsResetInProgress { get; private set; }

        public GameplayResetOrchestrator(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public async Task<bool> RequestResetAsync(GameplayResetRequest request)
        {
            if (!await _mutex.WaitAsync(0))
            {
                DebugUtility.LogWarning(typeof(GameplayResetOrchestrator),
                    $"Gameplay reset ignored (in progress). request={request}");
                return false;
            }

            IsResetInProgress = true;
            try
            {
                EnsureDependencies();

                int serial = Interlocked.Increment(ref _requestSerial);
                string reason = string.IsNullOrWhiteSpace(request.Reason) ? "GameplayReset/Request" : request.Reason;

                var normalized = new GameplayResetRequest(request.Target, reason, request.ActorIds);

                DebugUtility.Log(typeof(GameplayResetOrchestrator),
                    $"[GameplayReset] Start: {normalized} (scene='{GetEffectiveSceneName()}', serial={serial})");

                BuildTargets(normalized);

                if (_targets.Count == 0)
                {
                    DebugUtility.LogVerbose(typeof(GameplayResetOrchestrator),
                        $"[GameplayReset] No targets resolved. request={normalized}");
                    return true;
                }

                foreach (var target in _targets)
                {
                    await ResetOneTargetAsync(target, normalized, serial);
                }

                DebugUtility.Log(typeof(GameplayResetOrchestrator),
                    $"[GameplayReset] Completed: {normalized} (targets={_targets.Count}, serial={serial})");

                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameplayResetOrchestrator),
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
            var scene = GetEffectiveSceneName();

            provider.TryGetForScene(scene, out _actorRegistry);

            if (!provider.TryGetForScene(scene, out _classifier) || _classifier == null)
            {
                _classifier = new DefaultGameplayResetTargetClassifier();
            }

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

        private void BuildTargets(GameplayResetRequest request)
        {
            _targets.Clear();

            // 1) Tenta via ActorRegistry + classifier (rápido, determinístico)
            if (_actorRegistry != null)
            {
                _actorBuffer.Clear();
                _classifier.CollectTargets(request, _actorRegistry, _actorBuffer);

                if (_actorBuffer.Count > 0)
                {
                    foreach (var t in _actorBuffer)
                    {
                        TryAddTargetFromActor(t);
                    }

                    SortTargets();
                    return;
                }

                // Importante: se registry existe, mas está vazio (ou classifier não achou),
                // fazemos fallback por scan para TODOS os objetivos.
            }

            // 2) Fallback (sem registry ou sem dados): scan da cena por IActor.
            CollectTargetsBySceneScan(request);
            SortTargets();
        }

        private void CollectTargetsBySceneScan(GameplayResetRequest request)
        {
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            if (behaviours == null || behaviours.Length == 0)
            {
                return;
            }

            string sceneName = GetEffectiveSceneName();

            HashSet<string> ids = null;
            if (request is { Target: GameplayResetTarget.ActorIdSet, ActorIds: { Count: > 0 } })
            {
                ids = new HashSet<string>(
                    request.ActorIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
            }

            foreach (var mb in behaviours)
            {
                if (mb is not IActor actor)
                {
                    continue;
                }

                if (mb.gameObject == null || mb.gameObject.scene.name != sceneName)
                {
                    continue;
                }

                if (request.Target == GameplayResetTarget.ActorIdSet)
                {
                    if (ids == null || !ids.Contains(actor.ActorId ?? string.Empty))
                    {
                        continue;
                    }
                }
                else if (request.Target == GameplayResetTarget.PlayersOnly)
                {
                    if (mb.GetComponent<PlayerActor>() == null)
                    {
                        continue;
                    }
                }
                else if (request.Target == GameplayResetTarget.EaterOnly)
                {
                    if (mb.GetComponent("EaterActor") == null)
                    {
                        continue;
                    }
                }

                TryAddTargetFromActor(actor);
            }
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

            var actorId = actor.ActorId ?? string.Empty;
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

        private async Task ResetOneTargetAsync(ResetTarget target, GameplayResetRequest request, int serial)
        {
            var actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;

            var components = ResolveResettableComponents(target, request);
            if (components.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(GameplayResetOrchestrator),
                    $"[GameplayReset] No resettable components for actorId={actorId}.");
                return;
            }

            await RunPhaseAsync(components, GameplayResetPhase.Cleanup, request, serial);
            await RunPhaseAsync(components, GameplayResetPhase.Restore, request, serial);
            await RunPhaseAsync(components, GameplayResetPhase.Rebind, request, serial);
        }

        private IReadOnlyList<ResetEntry> ResolveResettableComponents(ResetTarget target, GameplayResetRequest request)
        {
            _orderedResets.Clear();
            _resettableBuffer.Clear();

            var root = target.Root;
            if (root == null)
            {
                return Array.Empty<ResetEntry>();
            }

            var monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
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

                if (mb is IGameplayResettable resettable)
                {
                    if (resettable is IGameplayResetTargetFilter filter &&
                        !filter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(resettable);
                    continue;
                }

                if (mb is IGameplayResettableSync sync)
                {
                    if (sync is IGameplayResetTargetFilter filter &&
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
                var order = component is IGameplayResetOrder resetOrder ? resetOrder.ResetOrder : 0;
                _orderedResets.Add(new ResetEntry(component, order));
            }

            _orderedResets.Sort(CompareResetEntries);

            return _orderedResets;
        }

        private async Task RunPhaseAsync(IReadOnlyList<ResetEntry> components, GameplayResetPhase phase, GameplayResetRequest request, int serial)
        {
            var ctx = new GameplayResetContext(
                GetEffectiveSceneName(),
                request,
                serial,
                Time.frameCount,
                phase);

            foreach (var t in components)
            {
                await InvokePhaseAsync(t.Component, phase, ctx);
            }
        }

        private static Task InvokePhaseAsync(IGameplayResettable component, GameplayResetPhase phase, GameplayResetContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return phase switch
            {
                GameplayResetPhase.Cleanup => component.ResetCleanupAsync(ctx),
                GameplayResetPhase.Restore => component.ResetRestoreAsync(ctx),
                GameplayResetPhase.Rebind => component.ResetRebindAsync(ctx),
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
            public ResetEntry(IGameplayResettable component, int order)
            {
                Component = component;
                Order = order;
            }

            public IGameplayResettable Component { get; }
            public int Order { get; }
        }

        private sealed class SyncAdapter : IGameplayResettable
        {
            private readonly IGameplayResettableSync _sync;

            public SyncAdapter(IGameplayResettableSync sync)
            {
                _sync = sync;
            }

            public Task ResetCleanupAsync(GameplayResetContext ctx)
            {
                _sync.ResetCleanup(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRestoreAsync(GameplayResetContext ctx)
            {
                _sync.ResetRestore(ctx);
                return Task.CompletedTask;
            }

            public Task ResetRebindAsync(GameplayResetContext ctx)
            {
                _sync.ResetRebind(ctx);
                return Task.CompletedTask;
            }
        }
    }
}
