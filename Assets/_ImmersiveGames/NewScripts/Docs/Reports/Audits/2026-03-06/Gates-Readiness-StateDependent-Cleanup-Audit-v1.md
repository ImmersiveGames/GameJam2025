# Gates-Readiness-StateDependent Cleanup Audit v1 (GRS-1.1)

Date: 2026-03-06
Scope:
- `Modules/Gates/**`
- `Modules/SceneFlow/Readiness/**`
- `Modules/InputModes/**`
- `Modules/Gameplay/Runtime/Actions/States/**`
- read-only DI callsites in `Infrastructure/Composition/**`

## A) Canonical rail

### Texto
O trilho canônico atual inicia no `SceneTransitionStartedEvent` publicado por `SceneTransitionService`, passa por `GameReadinessService` (que adquire token de transição no `SimulationGateService` e publica snapshots), e em paralelo por `SceneFlowInputModeBridge` (que sincroniza InputMode/GameLoop no `Completed` com dedupe profile+signature). O `StateDependentService` consolida Gate + Readiness + estado do GameLoop para bloquear/liberar ações de gameplay sem assumir ownership de restart.

### Bullet flow
- `SceneTransitionService` publica `SceneTransitionStartedEvent`
- `GameReadinessService`:
  - `Acquire(SimulationGateTokens.SceneTransition)`
  - marca `gameplayReady=false`
  - publica `ReadinessChangedEvent`
- `SceneFlowInputModeBridge`:
  - reseta dedupe no `Started`
- `SceneTransitionService` publica `SceneTransitionCompletedEvent`
- `GameReadinessService`:
  - libera gate de transição
  - define readiness (`GameplayReady`/`NonGameplayReady`)
  - publica `ReadinessChangedEvent`
- `SceneFlowInputModeBridge`:
  - aplica InputMode + sync GameLoop com dedupe `profile|signature`
- `StateDependentService`:
  - consome pause/resume/exit/reset/readiness
  - decide `CanExecuteGameplayAction` por Gate + Readiness + loop state

## B) Inventário

| FilePath | Type (Runtime/Editor) | Canon/Legacy/Dead | Notes |
|---|---|---|---|
| Modules/Gates/SimulationGateService.cs | Runtime | Canon | Owner do gate por token com ref-count thread-safe. |
| Modules/Gates/ISimulationGateService.cs | Runtime | Canon | Contrato canônico do gate. |
| Modules/Gates/SimulationGateTokens.cs | Runtime | Canon | Tokens usados por pause/scene-transition/etc. |
| Modules/Gates/Interop/GamePauseGateBridge.cs | Runtime | Canon | Bridge pause/resume/exit -> token Pause no gate. |
| Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs | Runtime | Canon | Owner de readiness por SceneFlow + gate transition. |
| Modules/SceneFlow/Readiness/Runtime/ReadinessChangedEvent.cs | Runtime | Canon | Evento de snapshot readiness. |
| Modules/SceneFlow/Readiness/Runtime/ReadinessSnapshot.cs | Runtime | Canon | Payload readiness. |
| Modules/SceneFlow/Readiness/Runtime/IGameplaySceneClassifier.cs | Runtime | Canon | Contrato de classificação gameplay. |
| Modules/SceneFlow/Readiness/Runtime/DefaultGameplaySceneClassifier.cs | Runtime | Canon | Classificador padrão (marker + fallback). |
| Modules/SceneFlow/Readiness/Bindings/GameplaySceneMarker.cs | Runtime | Canon (requires manual confirmation) | MonoBehaviour por cena (wiring Unity). |
| Modules/SceneFlow/Readiness/Runtime/SceneScopeMarker.cs | Runtime | Canon (requires manual confirmation) | Marker de escopo por cena (wiring Unity). |
| Modules/SceneFlow/Readiness/Runtime/ISceneScopeMarker.cs | Runtime | Canon | Interface de marker de escopo. |
| Modules/InputModes/IInputModeService.cs | Runtime | Canon | Contrato canônico de input mode. |
| Modules/InputModes/InputModeService.cs | Runtime | Canon | Implementação canônica de action maps. |
| Modules/InputModes/Interop/SceneFlowInputModeBridge.cs | Runtime | Canon | Sync por SceneFlow Completed com dedupe `profile|signature`. |
| Modules/InputModes/Bindings/InputModeBootstrap.cs | Runtime | Candidate (requires manual confirmation) | Registro alternativo via MonoBehaviour/Awake. |
| Modules/Gameplay/Runtime/Actions/States/IStateDependentService.cs | Runtime | Canon | Contrato state-dependent. |
| Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs | Runtime | Canon | Consumer auxiliar de reset + gate/readiness-aware action gating. |

Movimentos para Legacy em GRS-1.1:
- Nenhum (não houve item 100% seguro pelas regras A/B/C).

## C) Evidência rg (trechos)

### Comandos obrigatórios executados
```text
rg -n "class\s+SimulationGateService|class\s+GameReadinessService|class\s+SceneFlowInputModeBridge|class\s+StateDependentService" Modules Infrastructure
rg -n "EventBus<SceneTransitionStartedEvent>|EventBus<SceneTransitionCompletedEvent>|EventBus<GameResetRequestedEvent>|EventBus<GamePauseCommandEvent>|EventBus<GameResumeRequestedEvent>|EventBus<GameExitToMenuRequestedEvent>" Modules Infrastructure
rg -n "RegisterIfMissing<ISimulationGateService>|RegisterPauseBridge|RegisterInputModeSceneFlowBridge|RegisterStateDependentService|GameReadinessService" Infrastructure/Composition
```

### Linhas relevantes
```text
Modules/Gates/SimulationGateService.cs:16 class SimulationGateService
Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs:17 class GameReadinessService
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:25 class SceneFlowInputModeBridge
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:22 class StateDependentService

Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:112 RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());
Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs:14 RegisterPauseBridge(...)
Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:63 RegisterInputModeSceneFlowBridge();
Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:65 RegisterStateDependentService();
Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs:64 new GameReadinessService(gateService)

Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:148 EventBus<SceneTransitionStartedEvent>.Raise(...)
Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:176 EventBus<SceneTransitionCompletedEvent>.Raise(...)
Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs:42 Register(SceneTransitionStartedEvent)
Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs:44 Register(SceneTransitionCompletedEvent)
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:37 Register(SceneTransitionStartedEvent)
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:38 Register(SceneTransitionCompletedEvent)

Modules/GameLoop/Commands/GameCommands.cs:72 EventBus<GameResetRequestedEvent>.Raise(...)
Modules/Navigation/Runtime/MacroRestartCoordinator.cs:33 EventBus<GameResetRequestedEvent>.Register(...)
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs:230 EventBus<GameResetRequestedEvent>.Register(...)
```

### Segurança (reset listener no GameLoop)
```text
rg -n "GameResetRequestedEvent" Modules/GameLoop Modules/Navigation Infrastructure/Composition
-> Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs: log "listener disabled"
-> Register ativo apenas em Modules/Navigation/Runtime/MacroRestartCoordinator.cs
```

## D) Candidatos para GRS-1.2

1. Consolidar estratégia de dedupe entre `SceneFlowInputModeBridge` e `GameReadinessService` (hoje chaves diferentes: `profile|signature` vs snapshot equality).
2. Revisar overlap de consumidores em pause/resume/exit (Gate bridge + StateDependent + GameLoop/PauseOverlay/Hotkey) para reduzir superfícies redundantes.
3. Revisar papel do consumer auxiliar de reset em `StateDependentService` (manter comportamento, mas explicitar contrato e limites frente ao owner de restart canônico).
4. Avaliar `InputModeBootstrap` vs registro global por composition (`RegisterInputModesFromRuntimeConfig`) e decidir desativação/legacy quando houver prova de wiring não utilizado.

## Observação sobre StateDependentService e reset

- Ao consumir `GameResetRequestedEvent`, o serviço **apenas**:
  - executa `SetState(ServiceState.Ready)` e
  - chama `SyncMoveDecisionLogIfChanged()`.
- Não dispara restart, não publica reset e não altera pipeline de navegação.
- Classificação nesta etapa: **consumer auxiliar**.
- Risco de overlap: baixo-médio (sobreposição semântica de “estado pronto” com owner de restart em Navigation), sem remoção em GRS-1.1.

## Behavior-preserving

- Nenhum contrato público alterado.
- Nenhum pipeline novo criado.
- Nenhum listener de reset reintroduzido em GameLoop.
- Sem moves para Legacy por ausência de segurança total (A/B/C).
