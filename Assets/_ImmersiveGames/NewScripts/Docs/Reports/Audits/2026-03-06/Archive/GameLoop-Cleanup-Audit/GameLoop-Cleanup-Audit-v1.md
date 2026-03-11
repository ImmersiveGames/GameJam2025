# GameLoop Cleanup Audit v1 (GL-1.1)

Date: 2026-03-06
Scope: `Modules/GameLoop/**` (read-only evidence from `Modules/Navigation/**`, `Modules/SceneFlow/**`, `Modules/LevelFlow/**`, `Infrastructure/Composition/**`).

## Status atual (Baseline 3.1 — NAO MEXER)

- Baseline 3.1 mantida sem mudanca de comportamento.
- Pipeline canonico em runtime continua com:
  - `IGameLoopService` (`GameLoopService`)
  - `IGameCommands` (`GameCommands`)
  - `IGameRunStateService` (`GameRunStateService`)
  - `IGameRunOutcomeService` (`GameRunOutcomeService`)
  - `GameLoopSceneFlowCoordinator` para sync de start/ready via SceneFlow.
- Reset canonico permanece fora de GameLoop runtime listeners:
  - publisher: `GameCommands` (`GameResetRequestedEvent`)
  - consumer owner: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`

## Ownership map

| Componente | Owner de | Nao-owner de | Logs ancora |
|---|---|---|---|
| `Runtime/Services/GameLoopService.cs` | Estado da run (Ready/Playing/Paused/PostPlay) e publish de `GameRunStartedEvent` | Macro restart | `[GameLoop] ...` state logs |
| `Commands/GameCommands.cs` | API de comandos definitivos (pause/resume/restart/exit/run-end request) | Consumo de `GameResetRequestedEvent` | `[GameCommands] RequestRestart ...` |
| `Runtime/Bridges/GameLoopCommandEventBridge.cs` | Consumo de pause/resume/exit para `IGameLoopService` | Listener de reset (explicitamente desativado) | `[OBS][LEGACY] GameResetRequestedEvent listener disabled ...` |
| `Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` | Sync SceneFlow+WorldLifecycle -> `RequestReady` no start plan | Trigger direto de start gameplay (fica no pipeline IntroStage/LevelFlow) | `[GameLoopSceneFlow] Sync concluído ... RequestReady()` |
| `Runtime/Services/GameRunOutcomeService.cs` | Publicacao idempotente de `GameRunEndedEvent` por run | Controle de overlay/UI | `[GameLoop] Publicando GameRunEndedEvent ...` |
| `Runtime/Services/GameRunStateService.cs` | Snapshot de outcome/reason para consumo de UI/sistemas | Publicacao de fim de run | `[GameLoop] GameRunStateService ...` |
| `IntroStage/Runtime/IntroStageCoordinator.cs` + `IntroStageControlService.cs` | Orquestracao de IntroStage (complete/skip/policy) | Selecao de level (LevelFlow) | `[IntroStageController] ...` |
| `Modules/PostGame/PostGameOwnershipService.cs` (integração) | Ownership de PostGame overlay/regras de transicao pos-fim | Core state machine do GameLoop | `[PostPlay] ...` |

## Inventario A/B/C (com evidencia)

### A) CANONICOS (baseline/pipeline)

- `Modules/GameLoop/Runtime/Services/GameLoopService.cs`
- `Modules/GameLoop/Runtime/Services/GameRunStateService.cs`
- `Modules/GameLoop/Runtime/Services/GameRunOutcomeService.cs`
- `Modules/GameLoop/Runtime/Services/GameRunEndRequestService.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs`
- `Modules/GameLoop/Runtime/Bridges/GameRunOutcomeCommandBridge.cs`
- `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
- `Modules/GameLoop/Commands/GameCommands.cs`
- `Modules/GameLoop/IntroStage/Runtime/*` + `Modules/GameLoop/IntroStage/IntroStageControlService.cs` + `IntroStageContracts.cs`
- `Modules/GameLoop/Bindings/Bootstrap/GameLoopBootstrap.cs`
- `Modules/GameLoop/Bindings/Drivers/GameLoopDriver.cs`
- `Modules/GameLoop/Bindings/Bridges/GameLoopRunEndEventBridge.cs`

Evidence (`rg`):
```text
Infrastructure/Composition/GlobalCompositionRoot.GameLoop.cs:22 GameLoopBootstrap.Ensure(...)
Infrastructure/Composition/GlobalCompositionRoot.GameLoop.cs:57 RegisterIfMissing<IGameCommands>(() => new GameCommands(...))
Infrastructure/Composition/GlobalCompositionRoot.GameLoop.cs:75 RegisterIfMissing<IGameRunStateService>(() => new GameRunStateService(...))
Infrastructure/Composition/GlobalCompositionRoot.GameLoop.cs:93 RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(...))
Infrastructure/Composition/GlobalCompositionRoot.Coordinator.cs:47 new GameLoopSceneFlowCoordinator(sceneFlow, startPlan)
Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:73 RegisterGameLoopSceneFlowCoordinatorIfAvailable()
Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:118 RegisterIntroStageCoordinator(); 119 RegisterIntroStageControlService();
Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:181 TryGetGlobal<IIntroStageCoordinator>(...)
```

### B) LEGACY/Compat (para mover nesta etapa)

- **Nenhum item confirmado com seguranca para mover em GL-1.1**.

Motivo:
- Os candidatos com baixa referencia no codigo (`MonoBehaviour` de bindings/dev/tools) podem ser acionados por cena/prefab/ContextMenu/Unity lifecycle.
- Sem evidencia conclusiva de inatividade no runtime baseline, mover poderia introduzir risco de serializacao/scene wiring.

### C) Dead candidates (NAO deletar em GL-1.1)

1. `Modules/GameLoop/Bindings/Bootstrap/GameStartRequestEmitter.cs`
- Evidencia de baixa referencia textual:
```text
rg -n --glob '!Docs/**' 'GameStartRequestEmitter' .
=> apenas definicao do proprio arquivo
```
- Observacao: possui `MonoBehaviour` + `RuntimeInitializeOnLoadMethod`; **requires manual confirmation**.

2. `Modules/GameLoop/Bindings/Inputs/GamePauseHotkeyController.cs`
- Evidencia de baixa referencia textual:
```text
scan local de nomes: POSSIBLE_SINGLE_REF GamePauseHotkeyController ... hits=1
```
- Observacao: `MonoBehaviour` de input pode estar em cena/prefab; **requires manual confirmation**.

3. `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`
- Evidencia:
```text
scan local de nomes: POSSIBLE_SINGLE_REF IntroStageDevTools ... hits=1
```
- Observacao: editor/dev-only; **requires manual confirmation**.

## Movimentos aplicados

- Nenhum move aplicado em GL-1.1.

## Double listeners / ownership check critico

### GameResetRequestedEvent

- Publisher: `Modules/GameLoop/Commands/GameCommands.cs:72`.
- Consumer canonico: `Modules/Navigation/Runtime/MacroRestartCoordinator.cs:33`.
- Em `Modules/GameLoop/**`: **nenhum Register ativo** para `GameResetRequestedEvent`.
- Evidencia:
```text
rg -n "GameResetRequestedEvent" Modules/GameLoop Modules/Navigation Infrastructure/Composition
-> GameLoopCommandEventBridge apenas loga "listener disabled"
-> Register/Unregister ativos apenas em MacroRestartCoordinator
```

### Eventos de start/ready/intro/playing

- `SceneFlowInputModeBridge` continua dono do sync de profile no `TransitionCompleted`.
- `LevelStageOrchestrator` continua trigger de IntroStage por `SceneTransitionCompleted` + `LevelSwapLocalAppliedEvent`.
- `GameLoopSceneFlowCoordinator` continua sincronizando start plan em `RequestReady` (nao `RequestStart`).
- Nenhuma duplicidade removida nesta etapa; se necessario, fica para GL-1.2 com decisao arquitetural.

## Validacao estatica (behavior-preserving)

Checks executados:
```text
rg -n "GameResetRequestedEvent" Modules/GameLoop Modules/Navigation Infrastructure/Composition
rg -n "GamePauseCommandEvent|GameResumeRequestedEvent|GameExitToMenuRequestedEvent|GameRunStartedEvent|GameRunEndedEvent" Modules/GameLoop Modules/Navigation Modules/SceneFlow Modules/LevelFlow Infrastructure/Composition
rg -n "GameLoopBootstrap|GameLoopSceneFlowCoordinator|GameRunOutcomeCommandBridge|GameLoopCommandEventBridge" Infrastructure/Composition Modules/GameLoop
```

Resultado:
- Nenhum listener de reset foi reintroduzido em `Modules/GameLoop/**`.
- Mapeamento de owners/callsites permaneceu inalterado.
- Declaracao: **cleanup GL-1.1 behavior-preserving (analise estatica)**.
