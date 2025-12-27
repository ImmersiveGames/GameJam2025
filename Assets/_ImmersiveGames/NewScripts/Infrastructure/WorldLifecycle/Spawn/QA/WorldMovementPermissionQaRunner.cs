using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Player.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn.QA
{
    /// <summary>
    /// QA runner (ContextMenu): valida o gating de movimento do Player no NewScripts.
    ///
    /// Fluxo esperado no "contexto Menu/baseline" (sem gameplay ativo):
    /// - CanExecuteAction(Move) deve retornar false.
    ///
    /// Fluxo com Player presente (spawnado por outro QA, ex.: WorldSpawnPipeline):
    /// 1) BaselineBlocked: injeta input sintético e confirma que o Player NÃO se desloca.
    /// 2) EnablePath: SetGameplayReady(true) + RequestStart() e confirma que o Player se desloca.
    ///
    /// Observação:
    /// - Este runner NÃO faz spawn. Você roda o spawn por outro QA e depois roda este runner.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldMovementPermissionQaRunner : MonoBehaviour
    {
        [Header("Wait for Player (when needed)")]
        [SerializeField] private float waitForPlayerTimeoutSeconds = 25f;

        [Header("Movement Probe")]
        [SerializeField] private Vector2 qaMoveInput = new Vector2(0f, 1f);
        [SerializeField] private int probeSteps = 10;
        [SerializeField] private float minDisplacement = 0.02f;

        [Header("Enable Path")]
        [SerializeField] private int extraGameLoopTicks = 8;

        [Header("Debug")]
        [SerializeField] private bool verbose = true;

        private ISimulationGateService _gate;
        private IStateDependentService _state;
        private GameReadinessService _readiness;
        private IGameLoopService _loop;

        private bool _running;
        private float _lastProbeDisplacement;

        [ContextMenu("QA/MovementPermission/Run")]
        public void RunFromContextMenu()
        {
            if (_running)
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>("[QA][MovementPermission] Runner já está em execução.");
                return;
            }

            _running = true;
            StartCoroutine(RunAsync());
        }

        [ContextMenu("QA/MovementPermission/MenuContext Only")]
        public void RunMenuContextOnlyFromContextMenu()
        {
            ResolveGlobals();
            RunMenuContextCheck();
        }

        private IEnumerator RunAsync()
        {
            ResolveGlobals();

            // 1 frame para garantir que bootstrap + bindings iniciais estabilizaram.
            yield return null;

            // Sempre valida o contexto inicial (Menu/baseline) antes de qualquer probe físico.
            RunMenuContextCheck();

            // Probes físicos exigem Player. Se já existe, não logamos "Waiting...".
            var movement = FindAnyMovementController();
            if (movement == null)
            {
                if (verbose)
                {
                    DebugUtility.Log<WorldMovementPermissionQaRunner>(
                        $"[QA][MovementPermission] Waiting for Player... timeout={waitForPlayerTimeoutSeconds:0.0}s (dispare o spawn por outro ContextMenu/QA).");
                }

                yield return WaitForPlayerAsync();
                movement = FindAnyMovementController();
            }

            if (movement == null)
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] Player não encontrado; probes físicos foram pulados. " +
                    "Rode o QA de spawn/pipeline (ex.: WorldSpawnPipeline) e rode este runner novamente.");
                _running = false;
                yield break;
            }

            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] Target => go='{movement.gameObject.name}', scene='{movement.gameObject.scene.name}', activeScene='{SceneManager.GetActiveScene().name}'.");
            }

            // Input determinístico: evita que o reader sobrescreva o input sintético.
            var reader = movement.GetComponent<NewPlayerInputReader>();
            if (reader != null)
            {
                reader.enabled = false;
            }

            yield return RunBaselineBlockedAsync(movement);
            yield return RunEnablePathAsync(movement);

            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>("[QA][MovementPermission] Completed.");
            }

            _running = false;
        }

        private void RunMenuContextCheck()
        {
            ResolveGlobals();

            bool allowed = _state == null || _state.CanExecuteAction(ActionType.Move);

            if (verbose)
            {
                LogWiring("MenuContext");
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] MenuContext => CanExecuteAction(Move)={allowed} (esperado: false no menu/baseline).");
            }

            if (allowed)
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] MenuContext => Move está LIBERADO no contexto inicial. " +
                    "Confirme se GameplayReady=false no snapshot inicial e se GameLoop não está em Playing.");
                return;
            }

            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>("[QA][MovementPermission] MenuContext => PASS (Move bloqueado).");
            }
        }

        private IEnumerator WaitForPlayerAsync()
        {
            float elapsed = 0f;
            float timeout = Mathf.Max(0.1f, waitForPlayerTimeoutSeconds);

            while (elapsed < timeout)
            {
                if (FindAnyMovementController() != null)
                {
                    if (verbose)
                    {
                        DebugUtility.Log<WorldMovementPermissionQaRunner>(
                            $"[QA][MovementPermission] Player encontrado (t={elapsed:0.00}s).");
                    }

                    yield break;
                }

                ResolveGlobals();
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator RunBaselineBlockedAsync(NewPlayerMovementController movement)
        {
            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] Phase 1/2 => BaselineBlocked (probe físico, esperado: NÃO mover).");
            }

            bool allowed = _state == null || _state.CanExecuteAction(ActionType.Move);

            if (verbose)
            {
                LogWiring("BaselineBlocked");
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] BaselineBlocked => CanExecuteAction(Move)={allowed} (esperado: false em baseline).");
            }

            yield return ProbeMovementAsync(movement, "BaselineBlocked");
            float displacement = _lastProbeDisplacement;

            if (displacement >= minDisplacement)
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] BaselineBlocked => OBS: Player MOVEU (displacement={displacement:0.###}). " +
                    "Isso indica que a cena já estava permissiva (Playing+Ready) ou o gating não está ativo.");
            }
            else
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] BaselineBlocked => PASS (displacement={displacement:0.###}, threshold={minDisplacement:0.###}).");
            }
        }

        private IEnumerator RunEnablePathAsync(NewPlayerMovementController movement)
        {
            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] Phase 2/2 => EnablePath (esperado: LIBERADO e mover).");
            }

            ResolveGlobals();

            if (_readiness != null)
            {
                _readiness.SetGameplayReady(true, "QA/MovementPermission/EnablePath");
                if (verbose)
                {
                    DebugUtility.Log<WorldMovementPermissionQaRunner>(
                        "[QA][MovementPermission] EnablePath => GameReadinessService.SetGameplayReady(true) chamado.");
                }
            }
            else
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] EnablePath => GameReadinessService não disponível no DI global; não foi possível SetGameplayReady(true).");
            }

            if (_loop != null)
            {
                _loop.RequestStart();
                if (verbose)
                {
                    DebugUtility.Log<WorldMovementPermissionQaRunner>(
                        "[QA][MovementPermission] EnablePath => IGameLoopService.RequestStart() chamado.");
                }
            }
            else
            {
                DebugUtility.LogWarning<WorldMovementPermissionQaRunner>(
                    "[QA][MovementPermission] EnablePath => IGameLoopService não disponível no DI global; não foi possível RequestStart().");
            }

            // Se a implementação concreta for GameLoopService, aplicamos ticks extras para garantir progressão.
            if (_loop is GameLoopService concreteLoop && extraGameLoopTicks > 0)
            {
                for (int i = 0; i < extraGameLoopTicks; i++)
                {
                    concreteLoop.Tick(0.016f);
                    yield return null;
                }
            }
            else
            {
                // Mesmo sem tick manual, damos alguns frames para drivers existentes.
                for (int i = 0; i < 3; i++)
                {
                    yield return null;
                }
            }

            bool allowed = _state == null || _state.CanExecuteAction(ActionType.Move);

            if (verbose)
            {
                LogWiring("EnablePath");
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] EnablePath => CanExecuteAction(Move)={allowed} (esperado: true).");
            }

            yield return ProbeMovementAsync(movement, "EnablePath");
            float displacement = _lastProbeDisplacement;
            bool moved = displacement >= minDisplacement;

            if (!allowed || !moved)
            {
                DebugUtility.LogError<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] FAIL => Move não liberado ou Player não se deslocou. " +
                    $"allowed={allowed}, displacement={displacement:0.###}, threshold={minDisplacement:0.###}. " +
                    "Ações: (1) confirme SetGameplayReady(true) foi aplicado, " +
                    "(2) confirme GameLoop avançou para Playing, e " +
                    "(3) confirme gate está aberto (IsOpen=true, ActiveTokenCount=0).");
                yield break;
            }

            DebugUtility.Log<WorldMovementPermissionQaRunner>(
                $"[QA][MovementPermission] PASS => Move liberado e Player deslocou (displacement={displacement:0.###}).");
        }

        private IEnumerator ProbeMovementAsync(NewPlayerMovementController movement, string label)
        {
            _lastProbeDisplacement = 0f;

            if (movement == null)
            {
                yield break;
            }

            var startPos = movement.transform.position;

            for (int i = 0; i < Mathf.Max(1, probeSteps); i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                movement.QA_SetMoveInput(qaMoveInput);
#endif
                yield return null;
                yield return new WaitForFixedUpdate();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            movement.QA_ClearInputs();
#endif
            var endPos = movement.transform.position;
            _lastProbeDisplacement = Vector3.Distance(startPos, endPos);

            if (verbose)
            {
                DebugUtility.Log<WorldMovementPermissionQaRunner>(
                    $"[QA][MovementPermission] {label} => displacement={_lastProbeDisplacement:0.###} (start={startPos}, end={endPos}).");
            }
        }

        private void ResolveGlobals()
        {
            if (DependencyManager.Provider == null)
            {
                return;
            }

            if (_gate == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gate);
            }

            if (_state == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _state);
            }

            if (_readiness == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _readiness);
            }

            if (_loop == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _loop);
            }
        }

        private NewPlayerMovementController FindAnyMovementController()
        {
            var all = FindObjectsByType<NewPlayerMovementController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return null;
            }

            foreach (var t in all)
            {
                if (t != null && t.isActiveAndEnabled)
                {
                    return t;
                }
            }

            return all[0];
        }

        private void LogWiring(string phase)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            string loopState = _loop != null ? _loop.CurrentStateIdName : "<null>";
            bool gateOpen = _gate?.IsOpen ?? true;
            int tokens = _gate?.ActiveTokenCount ?? 0;
            bool hasReadiness = _readiness != null;

            DebugUtility.Log<WorldMovementPermissionQaRunner>(
                $"[QA][MovementPermission] Wiring({phase}) => activeScene='{activeScene}', gateOpen={gateOpen}, activeTokens={tokens}, gameLoopState='{loopState}', readinessService={hasReadiness}.");
        }
    }
}
