# Gates-Readiness-StateDependent

## Status atual (2026-03-06)

- Baseline 3.1 mantida (sem alteração de semântica).
- Trilho canônico do módulo continua distribuído em quatro owners:
  - Gate infra: `Modules/Gates/SimulationGateService.cs`
  - Readiness de transição: `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
  - InputMode sync por SceneFlow: `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
  - Action gating dependente de estado: `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`

## Canonical Runtime Rail

- `SceneTransitionStartedEvent` (publisher: `SceneTransitionService`) marca início de transição.
- `GameReadinessService` adquire token `SimulationGateTokens.SceneTransition`, publica snapshot de readiness e mantém `gameplayReady=false`.
- `SceneFlowInputModeBridge` reseta dedupe no Started e no Completed aplica InputMode/GameLoop conforme profile+signature.
- `SceneTransitionCompletedEvent` libera o gate de transição e fecha decisão de readiness (`GameplayReady` vs `NonGameplayReady`).
- `StateDependentService` combina Gate + Readiness + GameLoop state para permitir/bloquear ações (especialmente `Move`).

## Ownership por responsabilidade

| Domínio | Owner canônico | Não-owner |
|---|---|---|
| Gate tokens/ref-count | `SimulationGateService` | Regras de InputMode/Readiness |
| Bridge pause/resume -> gate | `GamePauseGateBridge` | Estado de gameplay/readiness |
| Readiness por SceneFlow | `GameReadinessService` | InputMode aplicado e restart macro |
| InputMode por transição | `SceneFlowInputModeBridge` | Gate infra e reset world |
| Blocking de ações in-game | `StateDependentService` | Execução de restart (macro) |

## Event Ownership Matrix

### SceneTransitionStartedEvent
- Publisher:
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (`EventBus<SceneTransitionStartedEvent>.Raise`)
- Consumers relevantes:
  - `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` (`Register`)
  - `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` (`Register`)
  - consumidores cross-módulo ativos (não owners deste módulo): `GameLoopSceneFlowCoordinator`, `SceneFlowSignatureCache`, `LoadingHudOrchestrator`.

### SceneTransitionCompletedEvent
- Publisher:
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (`EventBus<SceneTransitionCompletedEvent>.Raise`)
- Consumers relevantes:
  - `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` (`Register`)
  - `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` (`Register`)
  - consumidores cross-módulo ativos: `GameLoopSceneFlowCoordinator`, `LevelStageOrchestrator`, `SceneFlowSignatureCache`, `LoadingHudOrchestrator`.

### GameResetRequestedEvent
- Publisher:
  - `Modules/GameLoop/Commands/GameCommands.cs` (`EventBus<GameResetRequestedEvent>.Raise`)
- Consumers:
  - owner canônico de restart: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`
  - consumer auxiliar adicional: `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- Nota importante:
  - `StateDependentService` **não executa restart**; ao consumir reset ele só ajusta estado interno para `Ready` e recalcula logs de bloqueio/liberação de ação.

## Dedupe documentado (sem consolidar nesta etapa)

- `SceneFlowInputModeBridge`:
  - chave de dedupe: `"{profile}|{signature}"`
  - reset da dedupe no `SceneTransitionStartedEvent`
  - bypass explícito em estado `Boot` para ciclo de restart/boot.

- `GameReadinessService`:
  - dedupe por snapshot (`gameplayReady`, `gateOpen`, `activeTokens`)
  - campo `reason` não participa da igualdade para evitar publish redundante.

## Redundancy candidates

- Overlap de consumidores em `GamePauseCommandEvent`/`GameResumeRequestedEvent`/`GameExitToMenuRequestedEvent`:
  - `GamePauseGateBridge`, `StateDependentService`, `GameLoopCommandEventBridge`, `PauseOverlayController`, `GamePauseHotkeyController`.
- Overlap funcional parcial em `SceneTransitionCompletedEvent` entre `GameReadinessService` e `SceneFlowInputModeBridge` (ambos afetam estado de jogabilidade por caminhos distintos).
- `GameResetRequestedEvent` com consumer auxiliar (`StateDependentService`) além do owner de restart (`MacroRestartCoordinator`) — manter em GRS-1.1, decidir em GRS-1.2.

## Requires manual confirmation

- `Modules/InputModes/Bindings/InputModeBootstrap.cs`:
  - possível via wiring de cena/prefab (`MonoBehaviour` + `Awake`), apesar do registro canônico ocorrer em `GlobalCompositionRoot.RegisterInputModesFromRuntimeConfig`.
- `Modules/SceneFlow/Readiness/Bindings/GameplaySceneMarker.cs` e `Runtime/SceneScopeMarker.cs`:
  - dependem de presença em cena; não conclusivo por análise textual apenas.

## GRS-1.2 (hardening mínimo, behavior-preserving)

- Ownership mantido:
  - `GameReadinessService` segue owner de readiness/gate de transição.
  - `SceneFlowInputModeBridge` segue limitado a InputMode/GameLoop sync.
  - `StateDependentService` segue como consumer auxiliar de reset (sem ownership de restart).
- Hardening aplicado:
  - `SceneFlowInputModeBridge`: dedupe same-frame no `SceneTransitionStartedEvent` com log `[OBS][GRS]`.
  - `StateDependentService`: dedupe same-frame (`reason+frame`) no `GameResetRequestedEvent` com log `[OBS][GRS]`.
- Evidência completa: `Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent-Cleanup-Audit-v2.md`.
