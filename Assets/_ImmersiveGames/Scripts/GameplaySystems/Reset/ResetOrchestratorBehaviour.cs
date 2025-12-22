using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.GameplaySystems.Reset
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-130)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ResetOrchestratorBehaviour : MonoBehaviour, IResetOrchestrator
    {
        [Header("Event Wiring")]
        [Tooltip("Se ligado, o Orchestrator também escuta GameResetRequestedEvent. " +
                 "Desligue para evitar conflito com fluxos macro de reset (GameManager/MenuContext).")]
        [SerializeField] private bool listenToGameResetRequestedEvent = false;

        [Header("Scene-level Participants")]
        [Tooltip("Se ligado, executa reset também em participantes da cena (ex.: GameTimer), mesmo que não sejam filhos de um IActor-alvo.")]
        [SerializeField] private bool includeSceneLevelParticipants = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logVerbose = true;

        [Tooltip("Inclui participantes em filhos do root do ator.")]
        [SerializeField] private bool includeChildren = true;

        [Tooltip("Inclui GameObjects inativos na coleta de participantes.")]
        [SerializeField] private bool includeInactive = true;

        [Tooltip("Se verdadeiro, continua o reset mesmo se algum participante falhar (best-effort).")]
        [SerializeField] private bool bestEffort = true;

        private string _sceneName;

        private IActorRegistry _actorRegistry;
        private IPlayerDomain _playerDomain;
        private IEaterDomain _eaterDomain;
        private ISimulationGateService _gate;

        private int _requestSerial;
        private bool _inProgress;

        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;

        // Buffers para reduzir GC
        private readonly List<IActor> _targets = new(64);
        private readonly List<MonoBehaviour> _monoBuffer = new(256);
        private readonly List<object> _participants = new(256); // IResetInterfaces or IResetParticipantSync
        private readonly List<Exception> _errors = new(16);

        // Scene-level buffers
        private readonly List<GameObject> _sceneRoots = new(64);
        private readonly List<object> _sceneParticipants = new(256);

        public bool IsResetInProgress => _inProgress;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            // Registra no DI por cena
            DependencyManager.Provider.RegisterForScene<IResetOrchestrator>(_sceneName, this, allowOverride: true);

            ResolveServices();

            if (logVerbose)
            {
                DebugUtility.LogVerbose<ResetOrchestratorBehaviour>(
                    $"ResetOrchestrator registrado para a cena '{_sceneName}'.");
            }
        }

        private void OnEnable()
        {
            if (!listenToGameResetRequestedEvent)
                return;

            _resetRequestedBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetRequestedBinding);
        }

        private void OnDisable()
        {
            if (_resetRequestedBinding != null)
            {
                EventBus<GameResetRequestedEvent>.Unregister(_resetRequestedBinding);
                _resetRequestedBinding = null;
            }
        }

        private void ResolveServices()
        {
            var provider = DependencyManager.Provider;

            provider.TryGetGlobal(out _gate);

            provider.TryGetForScene<IActorRegistry>(_sceneName, out _actorRegistry);
            provider.TryGetForScene<IPlayerDomain>(_sceneName, out _playerDomain);
            provider.TryGetForScene<IEaterDomain>(_sceneName, out _eaterDomain);

            if (_gate == null)
            {
                DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                    "ISimulationGateService não encontrado (global). Reset ficará desprotegido (sem gate).",
                    this);
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                    $"IActorRegistry não encontrado para a cena '{_sceneName}'. Reset não terá alvos.",
                    this);
            }
        }

        private void OnResetRequested(GameResetRequestedEvent evt)
        {
            _ = RequestResetAsync(new ResetRequest(ResetScope.AllActorsInScene, reason: "GameResetRequestedEvent"));
        }

        public Task<bool> RequestResetAsync(ResetRequest request)
        {
            // Política simples: se já existe reset rodando, ignora.
            if (_inProgress)
            {
                if (logVerbose)
                {
                    DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                        $"Reset ignorado: já existe reset em andamento. Request={request}",
                        this);
                }

                return Task.FromResult(false);
            }

            _inProgress = true;
            _requestSerial++;

            return RunResetAsync(request, _requestSerial);
        }

        private async Task<bool> RunResetAsync(ResetRequest request, int serial)
        {
            ResolveServices();

            _errors.Clear();

            int frame = Time.frameCount;
            var ctx = new ResetContext(_sceneName, request, serial, frame, ResetStructs.Cleanup);

            IDisposable gateHandle = null;

            try
            {
                if (_gate != null)
                {
                    gateHandle = _gate.Acquire(SimulationGateTokens.SoftReset);
                }

                EventBus<GameResetStartedEvent>.Raise(new GameResetStartedEvent());

                if (logVerbose)
                {
                    DebugUtility.LogVerbose<ResetOrchestratorBehaviour>(
                        $"[Reset] START => {ctx}");
                }

                BuildTargets(request);

                if (_targets.Count == 0)
                {
                    DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                        $"[Reset] Nenhum alvo encontrado para escopo {request.Scope} na cena '{_sceneName}'.",
                        this);
                }

                await RunPhaseAsync(ctx.WithPhase(ResetStructs.Cleanup), ResetStructs.Cleanup);
                await RunPhaseAsync(ctx.WithPhase(ResetStructs.Restore), ResetStructs.Restore);
                await RunPhaseAsync(ctx.WithPhase(ResetStructs.Rebind), ResetStructs.Rebind);

                if (_errors.Count > 0)
                {
                    DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                        $"[Reset] Concluído com {_errors.Count} erro(s). Veja logs acima.",
                        this);
                }

                EventBus<GameResetCompletedEvent>.Raise(new GameResetCompletedEvent());

                if (logVerbose)
                {
                    DebugUtility.LogVerbose<ResetOrchestratorBehaviour>(
                        $"[Reset] END => Serial={serial}, Errors={_errors.Count}");
                }

                return _errors.Count == 0;
            }
            catch (Exception ex)
            {
                _errors.Add(ex);

                DebugUtility.LogError<ResetOrchestratorBehaviour>(
                    $"[Reset] FAIL => Serial={serial} | Exceção: {ex}",
                    this);

                return false;
            }
            finally
            {
                try { gateHandle?.Dispose(); }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                        $"[Reset] Falha ao liberar token do gate: {ex.Message}",
                        this);
                }

                _inProgress = false;
                _targets.Clear();
                _participants.Clear();
                _monoBuffer.Clear();
                _sceneParticipants.Clear();
                _sceneRoots.Clear();
            }
        }

        private void BuildTargets(ResetRequest request)
        {
            _targets.Clear();

            switch (request.Scope)
            {
                case ResetScope.AllActorsInScene:
                    if (_actorRegistry?.Actors != null)
                    {
                        foreach (var a in _actorRegistry.Actors)
                        {
                            if (a != null) _targets.Add(a);
                        }
                    }
                    break;

                case ResetScope.PlayersOnly:
                    if (_playerDomain?.Players != null)
                    {
                        for (int i = 0; i < _playerDomain.Players.Count; i++)
                        {
                            var p = _playerDomain.Players[i];
                            if (p != null) _targets.Add(p);
                        }
                    }
                    break;

                case ResetScope.EaterOnly:
                    if (_eaterDomain?.Eater != null) _targets.Add(_eaterDomain.Eater);
                    break;

                case ResetScope.ActorIdSet:
                    if (request.ActorIds != null && request.ActorIds.Count > 0 && _actorRegistry != null)
                    {
                        for (int i = 0; i < request.ActorIds.Count; i++)
                        {
                            var id = request.ActorIds[i];
                            if (string.IsNullOrWhiteSpace(id)) continue;

                            if (_actorRegistry.TryGetActor(id, out var actor) && actor != null)
                                _targets.Add(actor);
                        }
                    }
                    break;
            }
        }

        private async Task RunPhaseAsync(ResetContext ctx, ResetStructs phase)
        {
            if (logVerbose)
            {
                DebugUtility.LogVerbose<ResetOrchestratorBehaviour>(
                    $"[Reset] Phase={phase} Targets={_targets.Count}");
            }

            // 1) Scene-level participants (ex.: GameTimer), uma vez por fase.
            if (includeSceneLevelParticipants)
            {
                CollectSceneLevelParticipants(ctx.Request.Scope);
                SortListByOrder(_sceneParticipants);

                for (int p = 0; p < _sceneParticipants.Count; p++)
                {
                    var participant = _sceneParticipants[p];
                    if (participant == null) continue;

                    try
                    {
                        await ExecuteParticipantPhaseAsync(participant, ctx, phase);
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(ex);

                        DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                            $"[Reset] Erro (scene-level) em '{participant.GetType().Name}' Phase={phase} | Ex={ex.Message}",
                            this);

                        if (!bestEffort) throw;
                    }
                }

                _sceneParticipants.Clear();
                await Task.Yield();
            }

            // 2) Actor-level participants (por alvo).
            for (int i = 0; i < _targets.Count; i++)
            {
                var actor = _targets[i];
                if (actor == null) continue;

                var root = actor.Transform != null ? actor.Transform.gameObject : null;
                if (root == null) continue;

                CollectParticipants(root, ctx.Request.Scope);
                SortParticipantsByOrder();

                for (int p = 0; p < _participants.Count; p++)
                {
                    var participant = _participants[p];
                    if (participant == null) continue;

                    try
                    {
                        await ExecuteParticipantPhaseAsync(participant, ctx, phase);
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(ex);

                        DebugUtility.LogWarning<ResetOrchestratorBehaviour>(
                            $"[Reset] Erro em '{participant.GetType().Name}' | Actor='{actor.ActorName}' Phase={phase} | Ex={ex.Message}",
                            this);

                        if (!bestEffort) throw;
                    }
                }

                _participants.Clear();
                await Task.Yield();
            }
        }

        private void CollectSceneLevelParticipants(ResetScope scope)
        {
            _sceneParticipants.Clear();
            _sceneRoots.Clear();
            _monoBuffer.Clear();

            Scene scene = SceneManager.GetSceneByName(_sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            scene.GetRootGameObjects(_sceneRoots);

            for (int r = 0; r < _sceneRoots.Count; r++)
            {
                var root = _sceneRoots[r];
                if (root == null) continue;

                // Se esse root é um ator, ele será resetado no loop de targets (evita duplicar).
                if (root.GetComponent<IActor>() != null)
                    continue;

                _monoBuffer.Clear();

                if (includeChildren)
                    root.GetComponentsInChildren(includeInactive, _monoBuffer);
                else
                    root.GetComponents(_monoBuffer);

                for (int i = 0; i < _monoBuffer.Count; i++)
                {
                    var mb = _monoBuffer[i];
                    if (mb == null) continue;

                    if (mb is IResetScopeFilter filter && !filter.ShouldParticipate(scope))
                        continue;

                    if (mb is IResetInterfaces)
                    {
                        _sceneParticipants.Add(mb);
                        continue;
                    }

                    if (mb is IResetParticipantSync)
                        _sceneParticipants.Add(mb);
                }
            }
        }

        private void CollectParticipants(GameObject root, ResetScope scope)
        {
            _participants.Clear();
            _monoBuffer.Clear();

            if (includeChildren)
                root.GetComponentsInChildren(includeInactive, _monoBuffer);
            else
                root.GetComponents(_monoBuffer);

            for (int i = 0; i < _monoBuffer.Count; i++)
            {
                var mb = _monoBuffer[i];
                if (mb == null) continue;

                if (mb is IResetScopeFilter filter && !filter.ShouldParticipate(scope))
                    continue;

                if (mb is IResetInterfaces)
                {
                    _participants.Add(mb);
                    continue;
                }

                if (mb is IResetParticipantSync)
                    _participants.Add(mb);
            }
        }

        private void SortParticipantsByOrder()
        {
            SortListByOrder(_participants);
        }

        private static void SortListByOrder(List<object> list)
        {
            if (list == null || list.Count <= 1) return;

            list.Sort((a, b) =>
            {
                int oa = 0, ob = 0;
                if (a is IResetOrder ra) oa = ra.ResetOrder;
                if (b is IResetOrder rb) ob = rb.ResetOrder;

                int c = oa.CompareTo(ob);
                if (c != 0) return c;
                return string.CompareOrdinal(a.GetType().Name, b.GetType().Name);
            });
        }

        private static Task ExecuteParticipantPhaseAsync(object participant, ResetContext ctx, ResetStructs phase)
        {
            if (participant is IResetInterfaces asyncP)
            {
                return phase switch
                {
                    ResetStructs.Cleanup => asyncP.Reset_CleanupAsync(ctx),
                    ResetStructs.Restore => asyncP.Reset_RestoreAsync(ctx),
                    ResetStructs.Rebind => asyncP.Reset_RebindAsync(ctx),
                    _ => Task.CompletedTask
                };
            }

            if (participant is IResetParticipantSync syncP)
            {
                switch (phase)
                {
                    case ResetStructs.Cleanup: syncP.Reset_Cleanup(ctx); break;
                    case ResetStructs.Restore: syncP.Reset_Restore(ctx); break;
                    case ResetStructs.Rebind:  syncP.Reset_Rebind(ctx);  break;
                }

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
